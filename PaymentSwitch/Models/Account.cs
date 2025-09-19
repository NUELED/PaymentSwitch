using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using static PaymentSwitch.Utility.AppEnums;

namespace PaymentSwitch.Models
{
    [Table("Accounts")]
    public class Account
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public decimal Balance { get; set; }
        public string AccountNumber { get; set; }     //apply a unique constraint here in your query     
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AccountName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public decimal CurrentAccountBalance { get; set; }
        public AccountType AccountType { get; set; }
        public AccountTier AccountTier { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;

    }
}
