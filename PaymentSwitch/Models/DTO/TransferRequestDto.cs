namespace PaymentSwitch.Models.DTO
{
    public class TransferRequestDto
    {

        public string FromAccount { get; set; } = default!;
        public string ToBankCode { get; set; } = default!;
        public decimal Amount { get; set; }
        public string ToAccount { get; set; } = default!;
    }
}
