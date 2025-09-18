namespace PaymentSwitch.Utility
{
    public class NipResponse
    {
        public string ResponseCode { get; set; } = default!;
        public string ResponseMessage { get; set; } = default!;
        public bool IsSuccess => ResponseCode == ResponseCodes.Success; 
    }
}
