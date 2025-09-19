using Dapper;
using PaymentSwitch.Data.Abstraction;
using PaymentSwitch.Models;
using PaymentSwitch.Services.Abstraction;
using PaymentSwitch.Utility;
using System.Data;

namespace PaymentSwitch.Services.Implementation
{
    public class AccountService : IAccountService
    {
        private readonly IDapperRepository _dapperRepo;

        public AccountService(IDapperRepository dapperRepo)
        {
            _dapperRepo = dapperRepo;
        }

        public async Task<ApiResult> DebitAsync(string accountNumber, decimal amount, string refId)
        {
            var sql_GetAccount = Queries_StoredProcedures.FETCH_ACCOUNT;
            var sql_UpdateAccount_AferTransaction = Queries_StoredProcedures.UPDATE_ACCOUNT_AFTER_TRANSACTION;
            var sql_InsertTransaction = Queries_StoredProcedures.INSERT_ACCOUNT_TRANSACTION;

            var param = new DynamicParameters();
            param.Add("@AccountNumber", accountNumber);

            // 1. Get account
            var account = await _dapperRepo.QuerySingleAsync<Account>(sql_GetAccount, param);
            if (account == null)
                return new ApiResult { IsSuccess = false, Message = "Account not found" };

            if (account.Balance < amount)
                return new ApiResult { IsSuccess = false, Message = "Insufficient funds" };

            // 2. Update balance
            account.Balance -= amount;
            account.LastUpdated = DateTime.UtcNow;

            var updateParams = new DynamicParameters();
            updateParams.Add("@Balance", account.Balance);
            updateParams.Add("@LastUpdated", account.LastUpdated);
            updateParams.Add("@AccountNumber", account.AccountNumber);

            var rows = await _dapperRepo.ExecuteSqlAsync(sql_UpdateAccount_AferTransaction, updateParams);

            // inside DebitAsync after updating balance
            if (rows > 0)
            {
                var trxParams = new DynamicParameters();
                trxParams.Add("@AccountId", account.Id);
                trxParams.Add("@TransactionRef", refId);
                trxParams.Add("@Amount", amount);
                trxParams.Add("@TransactionType", "DEBIT");
                trxParams.Add("@TransactionDate", DateTime.UtcNow);
                trxParams.Add("@BalanceBefore", account.Balance + amount);  // before debit
                trxParams.Add("@BalanceAfter", account.Balance);            // after debit
                trxParams.Add("@Narration", $"Debit of {amount}");
                trxParams.Add("@Status", "Successful");

                await _dapperRepo.ExecuteSqlAsync(sql_InsertTransaction, trxParams);
            }

            return new ApiResult
            {
                IsSuccess = rows > 0,
                Message = rows > 0 ? $"Debited {amount} from {account.AccountNumber}" : "Debit failed",
                Result = account
            };
        }
        public async Task<ApiResult> DebitAsync_WithSp(string accountNumber, decimal amount, string refId)
        {
            var sp_params = new DynamicParameters();
            sp_params.Add("@AccountNumber", accountNumber);
            sp_params.Add("@Amount", amount);
            sp_params.Add("@RefId", refId);
            sp_params.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            sp_params.Add("@Message", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

            var result = await _dapperRepo.Execute_sp<int>("sp_DebitAccount", sp_params);

            return new ApiResult
            {
                IsSuccess = result == 1,
                Message = sp_params.Get<string>("@Message"),
                Result = result
            };
        }

        public async Task<ApiResult> CreditAsync(string accountNumber, decimal amount, string refId)
        {
            var sql_GetAccount = Queries_StoredProcedures.FETCH_ACCOUNT;
            var sql_UpdateAccount_AferTransaction = Queries_StoredProcedures.UPDATE_ACCOUNT_AFTER_TRANSACTION;
            var sql_InsertTransaction = Queries_StoredProcedures.INSERT_ACCOUNT_TRANSACTION;

            var param = new DynamicParameters();
            param.Add("@AccountNumber", accountNumber);

            // 1. Get account
            var account = await _dapperRepo.QuerySingleAsync<Account>(sql_GetAccount, param);
            if (account == null)
                return new ApiResult { IsSuccess = false, Message = "Account not found" };

            // 2. Update balance
            account.Balance += amount;
            account.LastUpdated = DateTime.UtcNow;

            var updateParams = new DynamicParameters();
            updateParams.Add("@Balance", account.Balance);
            updateParams.Add("@LastUpdated", account.LastUpdated);
            updateParams.Add("@AccountNumber", account.AccountNumber);

            var rows = await _dapperRepo.ExecuteSqlAsync(sql_UpdateAccount_AferTransaction, updateParams);

            // inside CreditAsync after updating balance
            if (rows > 0)
            {
                var trxParams = new DynamicParameters();
                trxParams.Add("@AccountId", account.Id);
                trxParams.Add("@TransactionRef", refId);
                trxParams.Add("@Amount", amount);
                trxParams.Add("@TransactionType", "CREDIT");
                trxParams.Add("@TransactionDate", DateTime.UtcNow);
                trxParams.Add("@BalanceBefore", account.Balance - amount);  // before credit
                trxParams.Add("@BalanceAfter", account.Balance);            // after credit
                trxParams.Add("@Narration", $"Credit of {amount}");
                trxParams.Add("@Status", "Successful");

                await _dapperRepo.ExecuteSqlAsync(sql_InsertTransaction, trxParams);
            }

            return new ApiResult
            {
                IsSuccess = rows > 0,
                Message = rows > 0 ? $"Credited {amount} to {account.AccountNumber}" : "Credit failed",
                Result = account
            };
        }

    }
}
