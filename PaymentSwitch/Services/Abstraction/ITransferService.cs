using PaymentSwitch.Models;
using PaymentSwitch.Utility;

namespace PaymentSwitch.Services.Abstraction
{
    public interface ITransferService
    {
        Task<ApiResult> InitiateTransferAsync(string fromAcc, string toAcc, string toBank, decimal amount, string tnxRef);
        Task HandlePendingTransactionAsync(Transfer t);

    }
}
