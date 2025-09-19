using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PaymentSwitch.Models
{
    [Table("AccountTransactions")]
    public class AccountTransaction
    {
        [Key]
        public long TransactionId { get; set; }   // BIGINT IDENTITY

        [Required]
        public string AccountId { get; set; }     // FK to Accounts.Id (GUID in string form)

        [Required]
        [MaxLength(50)]
        public string TransactionRef { get; set; }   // external ref (e.g., NIP Ref, UUID)

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string TransactionType { get; set; }  // Debit / Credit

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceBefore { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [MaxLength(255)]
        public string Narration { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";  // Pending, Successful, Failed, Reversed


        public DateTime? ProcessedAt { get; set; }
    }
}
