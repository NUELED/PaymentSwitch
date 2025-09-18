using PaymentSwitch.Utility;

namespace PaymentSwitch.Services.Abstraction
{
    public interface IAccountService
    {
        Task<ApiResult> DebitAsync(string accountNumber, decimal amount, string refId);
        Task<ApiResult> CreditAsync(string accountNumber, decimal amount, string refId);
    }
}
