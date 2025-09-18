using System.Security.Cryptography;

namespace PaymentSwitch.Utility
{
    public static class StaticData
    {

        #region MIBSS  Authentication
        public const string Scope = "scope";
        public const string ClientId = "client_id";
        public const string GrantType = "grant_type";
        public const string ClientSecret = "client_secret";
        public const string Value_ClientId = "250";
        #endregion

        #region Dynamic Configuration Variables
        public static string MibssLogin { get; set; }
        public const string LoginUrlFromAppsettings = "MibssUrls:LoginUrl";
        #endregion

        #region MIBSS API Endpoints
        public const string AddTransfer_endpoint = "/transfer";
    
        #endregion


        #region API Settings       
        public const string Secret = "Secret";
        public const string BvnUrl = "";
        public const string Audience = "Audience";
        public const string ApiSettings = "ApiSettings";

        public const string BvnCheck = "BvnCheck";
        public const string Issuer = "Issuer";
        public const string LogApiRequest = "Sending request to BVN Validation Service at {StartTime}, URL: {Url}";
        public const string LogApiResponse = "API Response received at {EndTime}, Duration: {Duration}ms";
        public const string LogApiResponseError = "API Error - Status Code: {StatusCode}, Content: {ErrorContent}";
        public const string ServiceTimeoutError = "The request to BVN Validation API timed out.";
        public const string ServiceError = "An error occurred while sending request to BVN Validation API.";

        #endregion

        #region HTTP Headers   
        public const string Basic = "Basic";
        public const string Bearer = "Bearer";
        public const string Accept = "Accept";
        public const string client_id = "client_id";
        public const string X_Version = "X-Version";
        public const string ApiVersion = "api-version";
        public const string Authorization = "Authorization";
        public const string RequestFormat = "application/json";
        public const string x_correlation_id = "x-correlation-id";
        #endregion

        #region General Constants    

        public const string PaymentIntegration = "PaymentIntegration";
        public const string MIBSSDateFormat = "yyyyMMdd";
        public const string Retry = "Retry";
        public const string DueTo = "due to";
        public const string StatusID = "StatusID";
        public const string StartDate = "StartDate";
        public const string EndDate = "EndDate";
        public const string OffSet = "Offset";
        public const string PageSize = "PageSize";
        public const string SchemeCode = "SchemeCode";
        public const string UserId = "UserId";
        public const string Success = "Success";
        public const string IsSuccess = "IsSuccess";
        public const string Status = "Status";
        public const string FailureReason = "FailureReason";
        public const string Ids = "Ids";
        public const string YES = "YES";
        public const string Failed = "Failed";
        public const string CacheKey = "NibssAccessToken";
        public const string NotFound = "Not Found";
        public const string Loginfailed = "Login failed";
        public const string Unauthorized = "Unauthorized";
        public const string AccessDenied = "Access Denied";
        public const string ErrorContent = "Error Content";
        public const string AnErrorOccurred = "An error occurred";
        public const string EmptyPayload = "Payload cannot be empty.";   
        public const string Con_Strings = "ConnectionStrings";
        public const string BG_Settings = "BackgroundSettings";
        public const string ApiCallLogMessage = "API call failed.";
        public const string DefaultConnection = "DefaultConnection";
        public const string ValidtionError = "Validation error occurred.";
        public const string NIBSSICADCredentials = "NIBSSICADCredentials";
        public const string DateRange = "The date range cannot exceed 30 days.";
        public const string HttpRequest_ErrorContent = "Error Content: {ErrorContent}";
        public const string UnknownError = "Unknown Error Occurred. Please try again later";
        public const string FailedToRetrieveAccessToken = "Failed to retrieve access token";       
        public const string RequestFailedWithStatusCode = "Request failed with status code";
        public const string BranchnfoValidtionError = "Validation failed for BranchInfo. Errors: {@Errors}";
        public const string AnErrorOccurredDuringRequestProcessing = "An error occurred during request processing.";
        public const string LogMessage_ApiRequest = "API Request - Time: {StartTime}, Endpoint: {Url}, Payload: {Payload}";
        public const string SecuritySchemeDescription = "Enter the Bearer Authorization string as following : `Bearer Generated-JWT-Token`";
        #endregion


        #region Logging      
        public const string Account = "Account";
        public const string LogFileName = "logs/ps_service.txt";
        public const string WorkerServiceLog = "worker-.log";
        public const string Logs = "logs";
     
        #endregion

     





        #region Private Methods  
        public static string GenerateRequestId()
        {
            // Step 1: Bank Code (6 digits)
            string bankCode = "123456";

            // Step 2: Date-Time in YYMMddHHmmss format (UTC)
            string dateTimePart = DateTime.UtcNow.ToString(MIBSSDateFormat);

            // Step 3: Secure 12-Digit Random Number
            string randomNumber = GenerateRandomDigits(12);

            // Combine all parts
            return $"{bankCode}{dateTimePart}{randomNumber}";
        }

        private static string GenerateRandomDigits(int length)
        {
            char[] digits = new char[length];

            for (int i = 0; i < length; i++)
            {
                digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10)); // Generates 0-9
            }

            return new string(digits);
        }
        #endregion
    }
}
