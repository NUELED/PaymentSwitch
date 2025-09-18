using static PaymentSwitch.Utility.AppEnums;

namespace PaymentSwitch.Models
{
    public class Transfer
    {
        public long Id { get; set; }
        public string TransactionRef { get; set; } = default!;
        public string FromAccount { get; set; } = default!;
        public string ToAccount { get; set; } = default!;
        public string ToBankCode { get; set; } = default!;
        public decimal Amount { get; set; }
        public TransferStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; }
        public string? Metadata { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
