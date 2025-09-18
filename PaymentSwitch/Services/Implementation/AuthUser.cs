using PaymentSwitch.Services.Abstraction;
using System.Security.Claims;

namespace PaymentSwitch.Services.Implementation
{
    public class AuthUser : IAuthUser
    {
        private readonly IHttpContextAccessor _accessor;

        public AuthUser(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public string ApiKey => Convert.ToString(_accessor.HttpContext.Items["TenantCode"]);
        public string RequestHash => Convert.ToString(_accessor.HttpContext.Items["RequestHash"]);
        public string Name => _accessor.HttpContext.User.FindFirst(ClaimTypes.Name).Value;


        public string EmailAddress => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        public bool Authenticated => _accessor.HttpContext.User.Identity.IsAuthenticated;

        public string UserId => _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
        public string UserCategory => _accessor.HttpContext.User.FindFirst("UserCategory")?.Value;
        public string FullName => _accessor.HttpContext.User.FindFirst("FullName")?.Value;
        public bool IsAuthenticated()
        {
            return _accessor.HttpContext.User.Identity.IsAuthenticated;
        }

        public string AuthToken => _accessor.HttpContext.Request.Headers.TryGetValue("Authorization", out var headerValue) ?
         headerValue.First().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1] : string.Empty;
        public IEnumerable<Claim> GetClaimsIdentity()
        {
            return _accessor.HttpContext.User.Claims;
        }
    }
}
