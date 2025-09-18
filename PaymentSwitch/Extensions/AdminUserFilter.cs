using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using PaymentSwitch.Services.Abstraction;
using System.Text;

namespace PaymentSwitch.Extensions
{
    public class AdminUserFilter : IAsyncActionFilter
    {
        private readonly IAuthUser _authUser;

        public AdminUserFilter(IAuthUser authUser)
        {
            _authUser = authUser;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_authUser.UserCategory == "AdminUser")
            {
                await next();
                return;
            }
            else
            {
                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.StatusCode = 403;
                await context.HttpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                {
                    statusCode = 403,
                    hasError = true,
                    result = string.Empty,
                    message = "Permission Denied: You do not have access to this resource",
                    count = 0
                })));
                return;
            }
        }
    }
}
