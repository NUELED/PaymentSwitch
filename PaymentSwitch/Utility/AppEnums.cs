namespace PaymentSwitch.Utility
{
    public class AppEnums
    {
 
        public enum StatusCodesEnum
        {
            OK = 200,
            Created = 201,
            Accepted = 202,
            NoContent = 204,
            Failed = 400,
            BadRequest = 400,
            Conflict = 409,
            NotFound = 404,
            TokenExpired = 406,
            NotAuthenticated = 401,
            ServerError = 500,

            PartialSuccess = 02,
            Nibss_Success = 00,
            Nibss_Failed = 99,
        }

        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }

        public enum TransferStatus { PendingDebitAttempt, Debited, Credited, Failed, Reversed, PendingQuery }

    }
}
