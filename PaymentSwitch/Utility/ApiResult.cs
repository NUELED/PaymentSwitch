using static PaymentSwitch.Utility.AppEnums;

namespace PaymentSwitch.Utility
{
    public class ApiResult<T>
    {
        public bool HasError { get; set; } = false;
        public string Message { get; set; }
        public StatusCodesEnum StatusCode { get; set; }
        public int Count { get; set; }
        public T Result { get; set; }


        public static ApiResult<T> SuccessMessage(StatusCodesEnum code = StatusCodesEnum.OK, string message = "", dynamic data = null, bool errorStatus = false, int count = 0)
        {
            return new ApiResult<T>
            {
                Result = (T)data,
                Message = message ?? StaticData.Success,
                HasError = errorStatus,
                StatusCode = code,
                Count = count
            };
        }
        public static ApiResult<T> ErrorMessage(StatusCodesEnum code, string message = "")
        {
            return new ApiResult<T>
            {
                Message = message,
                HasError = true,
                StatusCode = code,
                Count = 0
            };
        }
        public static ApiResult<T> SystemError(string message = "")
        {
            return new ApiResult<T>
            {
                Message = message ?? StaticData.UnknownError,
                HasError = false,
                StatusCode = StatusCodesEnum.ServerError,
            };
        }
    }

    public class ApiResult
    {
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; }
        public StatusCodesEnum StatusCode { get; set; }
        public int Count { get; set; }
        public object Result { get; set; }
    }
}
