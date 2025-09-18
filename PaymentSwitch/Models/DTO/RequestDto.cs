using PaymentSwitch.Utility;
using static PaymentSwitch.Utility.AppEnums;

namespace PaymentSwitch.Models.DTO
{
    public class RequestDto
    {
        public ApiType ApiType { get; set; } = ApiType.GET;
        public string Url { get; set; }
        public object? Data { get; set; }
    }
}
