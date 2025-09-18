using PaymentSwitch.Models.DTO;

namespace PaymentSwitch.Services.Abstraction
{
    public interface IBaseService
    {
        Task<ResponseDto?> SendAsync<T>(RequestDto requestDto, string accessToken = null);
    }
}
