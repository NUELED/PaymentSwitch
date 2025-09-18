using System.Security.Claims;

namespace PaymentSwitch.Services.Abstraction
{
    public interface IAuthUser
    {
        string ApiKey { get; }
        string RequestHash { get; }
        string Name { get; }
        string FullName { get; }
        string AuthToken { get; }
        string EmailAddress { get; }
        string UserId { get; }
        bool Authenticated { get; }
        bool IsAuthenticated();
        IEnumerable<Claim> GetClaimsIdentity();
        string UserCategory { get; }
    }
}
