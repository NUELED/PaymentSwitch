using PaymentSwitch.Models.DTO;
using PaymentSwitch.Services.Abstraction;
using PaymentSwitch.Utility;

namespace PaymentSwitch.Services.Implementation
{
    public class NipClient : INipClient
    {
        private readonly IBaseService _baseService; 
        private readonly string _nipBaseUrl;

        public NipClient(IBaseService baseService, IConfiguration config)
        {
            _baseService = baseService;
            _nipBaseUrl = config["ProcessorUrls:BaseUrl"] ?? throw new ArgumentNullException("Nip:BaseUrl not configured");
        }

        public async Task<NipResponse> SendTransferAsync(string transactionRef, string fromAcc, string toAcc, string toBank, decimal amount)
        {
            var payload = new
            {
                TransactionRef = transactionRef,
                FromAcc = fromAcc,
                ToAcc = toAcc,
                ToBank = toBank,
                Amount = amount
            };

            var request = new RequestDto
            {
                Url = $"{_nipBaseUrl}/api/nip/transfer",
                ApiType = AppEnums.ApiType.POST,
                Data = payload
            };

            var response = await _baseService.SendAsync<NipResponse>(request);

            return response?.IsSuccess == true
                ? (NipResponse)response.Result!
                : new NipResponse
                {
                    ResponseCode = "96",
                    ResponseMessage = response?.Message ?? "NETWORK_ERROR"
                };
        }

        public async Task<NipResponse> QueryAsync(string transactionRef)
        {
            var request = new RequestDto
            {
                Url = $"{_nipBaseUrl}/api/nip/query/{transactionRef}",
                ApiType = AppEnums.ApiType.GET
            };

            var response = await _baseService.SendAsync<NipResponse>(request);

            return response?.IsSuccess == true
                ? (NipResponse)response.Result!
                : new NipResponse
                {
                    ResponseCode = "96",
                    ResponseMessage = response?.Message ?? "NETWORK_ERROR"
                };
        }

        public async Task<NipResponse> ReverseAsync(string transactionRef)
        {
            var request = new RequestDto
            {
                Url = $"{_nipBaseUrl}/api/nip/reverse",
                ApiType = AppEnums.ApiType.POST,
                Data = new { TransactionRef = transactionRef }
            };

            var response = await _baseService.SendAsync<NipResponse>(request);

            return response?.IsSuccess == true
                ? (NipResponse)response.Result!
                : new NipResponse
                {
                    ResponseCode = "96",
                    ResponseMessage = response?.Message ?? "NETWORK_ERROR"
                };
        }

    }
}
