using PaymentSwitch.Utility;

namespace PaymentSwitch.Services.Abstraction
{
    public interface INipClient
    {
        Task<NipResponse> SendTransferAsync(string transactionRef, string fromAcc, string toAcc, string toBank, decimal amount);
        Task<NipResponse> QueryAsync(string transactionRef);
        Task<NipResponse> ReverseAsync(string transactionRef);
    }
}
