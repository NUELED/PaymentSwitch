using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PaymentSwitch.Data.Abstraction;
using PaymentSwitch.Models;
using System.Data;
using System.Security.Cryptography.Xml;

namespace PaymentSwitch.Data.Implementation
{
    public class TransferRepository : ITransferRepository
    {
        private readonly IDbConnection _db;
        public TransferRepository(IConfiguration configuration) => _db =  new SqlConnection(configuration.GetConnectionString("DefaultConnection"));

        public async Task CreateAsync(Transfer t)
        {
            var sql = @"INSERT INTO Transfers (TransactionRef, FromAccount, ToAccount, ToBankCode, Amount, Status, CreatedAt, UpdatedAt, RetryCount, Metadata)
                    VALUES (@TransactionRef,@FromAccount,@ToAccount,@ToBankCode,@Amount,@Status,@CreatedAt,@UpdatedAt,@RetryCount,@Metadata)"
            ;
            await _db.ExecuteAsync(sql, t);
        }

        public async Task<Transfer?> GetByRefAsync(string transactionRef)
        {
            var sql = "SELECT * FROM Transfers WHERE TransactionRef = @transactionRef";
            return await _db.QuerySingleOrDefaultAsync<Transfer>(sql, new { transactionRef });
        }

        public async Task UpdateAsync(Transfer t)
        {
            var sql = @"UPDATE Transfers SET Status=@Status, UpdatedAt=@UpdatedAt, RetryCount=@RetryCount, Metadata=@Metadata, ErrorMessage=@ErrorMessage
                    WHERE TransactionRef=@TransactionRef"
            ;
            await _db.ExecuteAsync(sql, t);
        }

        public async Task<IEnumerable<Transfer>> GetPendingAsync(int maxRetries, TimeSpan olderThan)
        {
            var cutoff = DateTime.UtcNow - olderThan;
            var sql = @"SELECT * FROM Transfers WHERE Status IN ('PendingDebitAttempt','PendingQuery') AND RetryCount < @maxRetries AND UpdatedAt < @cutoff";
            return await _db.QueryAsync<Transfer>(sql, new { maxRetries, cutoff });
        }
    }
}
