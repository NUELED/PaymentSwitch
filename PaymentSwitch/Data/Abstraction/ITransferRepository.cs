using PaymentSwitch.Models;
using System.Security.Cryptography.Xml;

namespace PaymentSwitch.Data.Abstraction
{
    public interface ITransferRepository
    {
        Task CreateAsync(Transfer t);
        Task<Transfer?> GetByRefAsync(string transactionRef);
        Task UpdateAsync(Transfer t);
        Task<IEnumerable<Transfer>> GetPendingAsync(int maxRetries, TimeSpan olderThan);
    }
}
