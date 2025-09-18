using PaymentSwitch.Models;
using PaymentSwitch.Utility;

namespace PaymentSwitch.Services.Abstraction
{
    public interface ITransferService
    {
        Task<string> InitiateTransferAsync(string fromAcc, string toAcc, string toBank, decimal amount);
        Task HandlePendingTransactionAsync(Transfer t);

    }
}
