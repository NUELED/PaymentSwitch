using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentSwitch.Models.DTO;
using PaymentSwitch.Services.Abstraction;
using PaymentSwitch.Utility;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;

namespace PaymentSwitch.Services.Implementation
{
    public class BaseService : IBaseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MIBSSCredentials _nibssICADCredentials;
        private readonly ILogger<BaseService> _logger;
        private readonly BackgroundSettings _background;
        private readonly IMemoryCache _memoryCache;
        private static readonly SemaphoreSlim _tokenLock = new(1, 1); // 1 at a time

        public BaseService(IHttpClientFactory httpClientFactory, IOptions<MIBSSCredentials> nibssICADCredentials, ILogger<BaseService> logger,
                           IMemoryCache memoryCache,
                           IOptions<BackgroundSettings> background)
        {
            _httpClientFactory = httpClientFactory;
            _nibssICADCredentials = nibssICADCredentials.Value;
            _logger = logger;
            _memoryCache = memoryCache;
            _background = background.Value;
        }

        public async Task<ResponseDto?> SendAsync<T>(RequestDto requestDto, string? accessToken = null)
        {
            var responseDto = new ResponseDto();

            //I defined a retry policy (retry up to 3 times, with exponential backoff)
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()             // network errors
                .Or<TaskCanceledException>()                // timeouts
                .Or<AuthenticationException>()              // SSL issues
                .OrResult(r => (int)r.StatusCode >= 500)    // server errors
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount} due to {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                    });

            // Circuit breaker policy
            var circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .Or<AuthenticationException>()
                .OrResult(r => (int)r.StatusCode >= 500)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, timespan) =>
                    {
                        _logger.LogError($"Circuit opened for {timespan.TotalSeconds}s due to {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                    },
                    onReset: () => _logger.LogInformation("Circuit closed. Service healthy again."),
                    onHalfOpen: () => _logger.LogInformation("Circuit half-open, testing...")
                );

            // Combine them (retry first, then circuit breaker)
            var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

            try
            {
                HttpClient client = _httpClientFactory.CreateClient();

                if (string.IsNullOrEmpty(accessToken))
                {
                    accessToken = await GetNibssAccessTokenAsync();
                }

                // HttpRequestMessage creation inside the retry policy
                HttpResponseMessage apiResponse = await combinedPolicy.ExecuteAsync(async () =>
                {
                    // Create fresh HttpRequestMessage for each retry attempt
                    using var message = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(requestDto.Url),
                        Method = requestDto.ApiType switch
                        {
                            AppEnums.ApiType.POST => HttpMethod.Post,
                            AppEnums.ApiType.DELETE => HttpMethod.Delete,
                            AppEnums.ApiType.PUT => HttpMethod.Put,
                            _ => HttpMethod.Get
                        }
                    };

                    // Add authorization header
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        message.Headers.Authorization = new AuthenticationHeaderValue(StaticData.Bearer, accessToken);
                    }

                    // Add accept header
                    message.Headers.Add(StaticData.Accept, StaticData.RequestFormat);

                    // Add content if provided
                    if (requestDto.Data != null)
                    {
                        message.Content = new StringContent(JsonConvert.SerializeObject(requestDto.Data), Encoding.UTF8, StaticData.RequestFormat);
                    }

                    // Send the request
                    return await client.SendAsync(message);
                });

                if (apiResponse.IsSuccessStatusCode)
                {
                    var apiContent = await apiResponse.Content.ReadAsStringAsync();
                    responseDto.IsSuccess = true;
                    responseDto.Result = JsonConvert.DeserializeObject<T>(apiContent);
                }
                else
                {
                    responseDto.IsSuccess = false;
                    responseDto.Message = apiResponse.StatusCode switch
                    {
                        HttpStatusCode.NotFound => StaticData.NotFound,
                        HttpStatusCode.Forbidden => StaticData.AccessDenied,
                        HttpStatusCode.Unauthorized => StaticData.Unauthorized,
                        HttpStatusCode.InternalServerError => StaticData.UnknownError,
                        _ => $"{StaticData.RequestFailedWithStatusCode}: {apiResponse.StatusCode}"
                    };

                    var errorContent = await apiResponse.Content.ReadAsStringAsync();
                    _logger.LogError(StaticData.HttpRequest_ErrorContent, errorContent);
                    responseDto.Result = errorContent;
                }
            }
            catch (Exception ex)
            {
                responseDto.IsSuccess = false;
                responseDto.Message = $"{StaticData.AnErrorOccurred}: {ex.Message}";
                _logger.LogError(ex, StaticData.ApiCallLogMessage);
            }
            return responseDto;
        }

   

        private async Task<string> GetNibssAccessTokenAsync()
        {
            const string tokenCacheKey = StaticData.CacheKey;

            // Try to get the token from the cache first
            if (_memoryCache.TryGetValue(tokenCacheKey, out string cachedToken))
            {
                return cachedToken;
            }

            // Lock to ensure only one thread fetches the token
            await _tokenLock.WaitAsync();
            try
            {
                // Double-check cache again after acquiring lock
                if (_memoryCache.TryGetValue(tokenCacheKey, out cachedToken))
                {
                    return cachedToken;
                }

                // Fetch token from NIBSS
                var client_id = _nibssICADCredentials.client_id;
                var client_secret = _nibssICADCredentials.client_secret;
                var grant_type = _nibssICADCredentials.grant_type;
                var scope = _nibssICADCredentials.scope;

                var formData = new Dictionary<string, string>
                {
                    { StaticData.client_id , client_id },
                    { StaticData.ClientSecret , client_secret },
                    { StaticData.GrantType, grant_type },
                    { StaticData.Scope, scope }
                };

                var loginRequest = new HttpRequestMessage(HttpMethod.Post, StaticData.MibssLogin)
                {
                    Content = new FormUrlEncodedContent(formData) //Add form-data to request body
                };

                HttpClient client = _httpClientFactory.CreateClient();
                HttpResponseMessage response = await client.SendAsync(loginRequest);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"{StaticData.Loginfailed}: {response.ReasonPhrase}");

                var startTime = DateTime.UtcNow;
                var url = StaticData.MibssLogin;

                var apiContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(StaticData.LogMessage_ApiRequest, startTime, url, JsonConvert.SerializeObject(response));

                var loginResponse = JsonConvert.DeserializeObject<NIBSSLoginResponse>(apiContent);
                var accessToken = loginResponse?.access_token
                                  ?? throw new Exception(StaticData.FailedToRetrieveAccessToken);

                // Cache it for 2 minutes
                _memoryCache.Set(tokenCacheKey, accessToken, TimeSpan.FromMinutes(_background.NibssTokenCacheTime));

                return accessToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }



    }
}
