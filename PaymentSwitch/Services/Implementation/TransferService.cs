using Newtonsoft.Json;
using PaymentSwitch.Data.Abstraction;
using PaymentSwitch.Models;
using PaymentSwitch.Services.Abstraction;
using PaymentSwitch.Utility;
using static PaymentSwitch.Utility.AppEnums;

namespace PaymentSwitch.Services.Implementation
{
    public class TransferService : ITransferService 
    {
        private readonly ITransferRepository _repo;
        private readonly INipClient _nip;
        private readonly ILogger<TransferService> _log;
        private readonly IAccountService _accountService;

        public TransferService(ITransferRepository repo, INipClient nip, ILogger<TransferService> log, IAccountService accountService)
        {
            _repo = repo; 
            _nip = nip; 
            _log = log;
            _accountService = accountService;   
        }

        /* 
        Flow Recap:

        Debit sender account first.

        If debit fails → transaction ends immediately.

        If NIP succeeds → keep debit, mark as Debited.

        If NIP uncertain (98/96) → keep debit, mark PendingQuery until reconciliation job decides.

        If NIP fails → reverse debit by crediting back.
        */

        public async Task<ApiResult> InitiateTransferAsync(string fromAcc, string toAcc, string toBank, decimal amount,string tnxRef)
        {
            var response = new ApiResult();

            //  Check for existing transaction (idempotency)
            var existing = await _repo.GetByRefAsync(tnxRef);
            if (existing != null)
            {
                response.IsSuccess = existing.Status == TransferStatus.Debited || existing.Status == TransferStatus.Credited;
                response.Message = $"Transaction already processed with status {existing.Status}";
                response.Result = existing; 
                return response;
            }

            var t = new Transfer
            {
                TransactionRef = tnxRef,
                FromAccount = fromAcc,
                ToAccount = toAcc,
                ToBankCode = toBank,
                Amount = amount,
                Status = TransferStatus.PendingDebitAttempt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(t);

            // 3. Debit locally
            var debitResult = await _accountService.DebitAsync(fromAcc, amount, tnxRef);

            if (!debitResult.IsSuccess)
            {
                t.Status = TransferStatus.Failed;
                t.ErrorMessage = debitResult.Message;
                t.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(t);

                return new ApiResult
                {
                    IsSuccess = false,
                    Message = $"Debit failed: {debitResult.Message}",
                    Result = t
                };
            }

            // 4. Call NIP outward transfer
            var nipResp = await _nip.SendTransferAsync(tnxRef, fromAcc, toAcc, toBank, amount);

            // 5. Handle NIP responses
            if (nipResp.IsSuccess)
            {
                t.Status = TransferStatus.Debited; // debit confirmed + transfer sent
                t.Metadata = JsonConvert.SerializeObject(nipResp);
                t.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(t);

                return new ApiResult
                {
                    IsSuccess = true,
                    Message = "Transfer initiated successfully",
                    Result = t
                };
            }

            if (nipResp.ResponseCode == "98" || nipResp.ResponseCode == "96")
            {
                // Uncertain → leave debit in place (money on hold)
                t.Status = TransferStatus.PendingQuery;
                t.ErrorMessage = nipResp.ResponseMessage;
                t.RetryCount++;
                t.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(t);

                return new ApiResult
                {
                    IsSuccess = true,
                    Message = "Transfer status pending. Please query later.",
                    Result = t
                };
            }

            // outright failure → reverse debit
            await _accountService.CreditAsync(fromAcc, amount, tnxRef);

            t.Status = TransferStatus.Failed;
            t.ErrorMessage = nipResp.ResponseMessage;
            t.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(t);

            return new ApiResult
            {
                IsSuccess = true,
                Message = $"Transfer failed: {nipResp.ResponseMessage}",
                Result = t
            };
        }

        // Handled by background job, but exposed for manual trigger
        public async Task HandlePendingTransactionAsync(Transfer t)
        {
            _log.LogInformation("Querying transaction {ref}", t.TransactionRef);
            var q = await _nip.QueryAsync(t.TransactionRef);
            if (q.IsSuccess)
            {
                t.Status = TransferStatus.Credited; // or Debited/Credited depending on payload field
                t.Metadata = JsonConvert.SerializeObject(q);
                t.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(t);
                return;
            }

            // If query shows debit succeeded but credit failed => reverse
            if (q.ResponseCode == "REVERSE_REQUIRED" || q.ResponseMessage.Contains("DEBITED_BUT_NOT_CREDITED"))
            {
                var rev = await _nip.ReverseAsync(t.TransactionRef);
                if (rev.IsSuccess)
                {
                    t.Status = TransferStatus.Reversed;
                    t.Metadata = AppendMetadata(t.Metadata, rev);
                }
                else
                {
                    t.ErrorMessage = $"Reversal failed: {rev.ResponseMessage}";
                    t.RetryCount++;
                }
                t.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(t);
                return;
            }

            // still pending or unknown -> increment retry count, leave status pending
            t.RetryCount++;
            t.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(t);
        }

        private string AppendMetadata(string? existing, NipResponse newResp)
        {
            var list = new List<NipResponse>();
            if (existing != null)
            {
                try { list = JsonConvert.DeserializeObject<List<NipResponse>>(existing) ?? list; } catch { }
            }
            list.Add(newResp);
            return JsonConvert.SerializeObject(list);
        }

    }

}
