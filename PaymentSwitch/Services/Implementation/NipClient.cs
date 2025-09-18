using PaymentSwitch.Services.Abstraction;
using PaymentSwitch.Utility;

namespace PaymentSwitch.Services.Implementation
{
    public class NipClient : INipClient
    {
        private readonly HttpClient _http;
        public NipClient(HttpClient http) { _http = http; }

        public async Task<NipResponse> SendTransferAsync(string transactionRef, string fromAcc, string toAcc, string toBank, decimal amount)
        {
            var payload = new { TransactionRef = transactionRef, FromAcc = fromAcc, ToAcc = toAcc, ToBank = toBank, Amount = amount };
            var res = await _http.PostAsJsonAsync("/api/nip/transfer", payload);
            if (!res.IsSuccessStatusCode)
            {
                if ((int)res.StatusCode == 504 || (int)res.StatusCode == 408) // timeout-ish
                    return new NipResponse { ResponseCode = "98", ResponseMessage = "TIMEOUT" };
                return new NipResponse { ResponseCode = "96", ResponseMessage = "NETWORK_ERROR" };
            }
            return await res.Content.ReadFromJsonAsync<NipResponse>() ?? new NipResponse { ResponseCode = "96", ResponseMessage = "InvalidResponse" };
        }

        public async Task<NipResponse> QueryAsync(string transactionRef)
        {
            var res = await _http.GetAsync($"/api/nip/query/{transactionRef}");
            return await res.Content.ReadFromJsonAsync<NipResponse>() ?? new NipResponse { ResponseCode = "96", ResponseMessage = "InvalidResponse" };
        }

        public async Task<NipResponse> ReverseAsync(string transactionRef)
        {
            var res = await _http.PostAsJsonAsync("/api/nip/reverse", new { TransactionRef = transactionRef });
            return await res.Content.ReadFromJsonAsync<NipResponse>() ?? new NipResponse { ResponseCode = "96", ResponseMessage = "InvalidResponse" };
        }
    }
}
