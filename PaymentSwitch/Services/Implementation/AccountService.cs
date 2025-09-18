using PaymentSwitch.Services.Abstraction;
using PaymentSwitch.Utility;

namespace PaymentSwitch.Services.Implementation
{
    public class AccountService : IAccountService
    {
        private readonly Dictionary<string, decimal> _balances = new();

        public AccountService()
        {
            // mock balances
            _balances["123"] = 10000m;
            _balances["456"] = 5000m;
        }

        public Task<ApiResult> DebitAsync(string accountNumber, decimal amount, string refId)
        {
            if (!_balances.ContainsKey(accountNumber))
                return Task.FromResult(new ApiResult { IsSuccess = false, Message = "Account not found" });

            if (_balances[accountNumber] < amount)
                return Task.FromResult(new ApiResult { IsSuccess = false, Message = "Insufficient funds" });

            _balances[accountNumber] -= amount;

            return Task.FromResult(new ApiResult
            {
                IsSuccess = true,
                Message = $"Debited {amount} from {accountNumber}",
                Result = _balances[accountNumber]
            });
        }

        public Task<ApiResult> CreditAsync(string accountNumber, decimal amount, string refId)
        {
            if (!_balances.ContainsKey(accountNumber))
                _balances[accountNumber] = 0;

            _balances[accountNumber] += amount;

            return Task.FromResult(new ApiResult
            {
                IsSuccess = true,
                Message = $"Credited {amount} to {accountNumber}",
                Result = _balances[accountNumber]
            });
        }


    }
}
