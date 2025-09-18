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

        public TransferService(ITransferRepository repo, INipClient nip, ILogger<TransferService> log)
        {
            _repo = repo; 
            _nip = nip; 
            _log = log;
        }

        public async Task<string> InitiateTransferAsync(string fromAcc, string toAcc, string toBank, decimal amount)
        {
            // create or get transactionRef for idempotency
            var txRef = Guid.NewGuid().ToString("N"); // ideally provided by caller for idempotency

            var existing = await _repo.GetByRefAsync(txRef);
            if (existing != null) return existing.TransactionRef;

            var t = new Transfer
            {
                TransactionRef = txRef,
                FromAccount = fromAcc,
                ToAccount = toAcc,
                ToBankCode = toBank,
                Amount = amount,
                Status = TransferStatus.PendingDebitAttempt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(t);

            // call NIP
            var nipResp = await _nip.SendTransferAsync(txRef, fromAcc, toAcc, toBank, amount);

            if (nipResp.IsSuccess)
            {
                t.Status = TransferStatus.Debited;
                t.Metadata = JsonConvert.SerializeObject(nipResp);
                await _repo.UpdateAsync(t);
                // Optionally trigger credit-check or mark as Credited after query
                return txRef;
            }

            if (nipResp.ResponseCode == "98" /*TIMEOUT*/ || nipResp.ResponseCode == "96")
            {
                // uncertain result: schedule for reconciliation
                t.Status = TransferStatus.PendingQuery;
                t.ErrorMessage = nipResp.ResponseMessage;
                t.RetryCount++;
                t.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(t);
                return txRef; // client sees pending
            }

            // Failed
            t.Status = TransferStatus.Failed;
            t.ErrorMessage = nipResp.ResponseMessage;
            t.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(t);
            return txRef;
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
