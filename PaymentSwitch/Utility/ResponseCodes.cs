namespace PaymentSwitch.Utility
{
    public static class ResponseCodes
    {
        public const string Success = "00";
        public const string PartialSuccess = "02";
        public const string InvalidBranchCode = "44";
        public const string InvalidBranchName = "45";
        public const string InvalidBranchLocation = "46";
        public const string InvalidRequestId = "54";
        public const string InvalidUniqueCustomerId = "55";
        public const string InvalidInstitutionCode = "56";
        public const string InvalidAccountNumber = "59";
        public const string InvalidAccountName = "60";
        public const string SecurityViolation = "63";
        public const string InvalidAccountDesignation = "64";
        public const string InvalidAccountStatus = "65";
        public const string InvalidAccountType = "66";
        public const string InvalidBVN = "67";
        public const string InvalidCurrency = "68";
        public const string InvalidRCNumber = "69";
        public const string JsonGenerationException = "86";
        public const string JsonMappingException = "87";
        public const string IOException = "88";
        public const string GeneralException = "89";
        public const string SystemMalfunctionOrInvalidEmail = "96";
        public const string RequestFailed = "99";
        public const string InvalidOldAccountNumber = "100";
        public const string InvalidPhoneNumber = "101";
        public const string InvalidTIN = "102";
        public const string InvalidPEP = "103";
        public const string InvalidSectorCode = "104";
        public const string LocalCurrencyRequiredForSavingsOrCurrent = "105";
        public const string UnknownResponseCode = "Unknown Response Code";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
          { Success, "The function call was successful" },
          { PartialSuccess, "Partial successful" },
          { InvalidBranchCode, "Invalid Branch Code" },
          { InvalidBranchName, "Invalid Branch Name" },
          { InvalidBranchLocation, "Invalid Branch Location" },
          { InvalidRequestId, "Invalid Request Id" },
          { InvalidUniqueCustomerId, "Invalid Unique Customer Id" },
          { InvalidInstitutionCode, "Invalid Institution Code" },
          { InvalidAccountNumber, "Invalid Account Number" },
          { InvalidAccountName, "Invalid Account Name" },
          { SecurityViolation, "Security Violation" },
          { InvalidAccountDesignation, "Invalid Account Designation" },
          { InvalidAccountStatus, "Invalid Account Status" },
          { InvalidAccountType, "Invalid Account Type" },
          { InvalidBVN, "Invalid BVN" },
          { InvalidCurrency, "Invalid Currency" },
          { InvalidRCNumber, "Invalid RC Number" },
          { JsonGenerationException, "JSON Generation Exception" },
          { JsonMappingException, "JSON Mapping Exception" },
          { IOException, "IO Exception" },
          { GeneralException, "Exception" },
          { SystemMalfunctionOrInvalidEmail, "System Malfunction / Invalid Email" },
          { RequestFailed, "Request Failed" },
          { InvalidOldAccountNumber, "Invalid Old Account Number" },
          { InvalidPhoneNumber, "Invalid Phone Number" },
          { InvalidTIN, "Invalid TIN" },
          { InvalidPEP, "Invalid PEP" },
          { InvalidSectorCode, "Invalid Sector Code" },
          { LocalCurrencyRequiredForSavingsOrCurrent, "Savings or current accounts must be in local currency" }
        };

        public static string GetDescription(string code)
        {
            return Descriptions.TryGetValue(code, out var description) ? description : UnknownResponseCode;
        }
    }

}
