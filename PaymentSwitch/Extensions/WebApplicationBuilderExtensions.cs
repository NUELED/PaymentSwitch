using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using PaymentSwitch.Data.Abstraction;
using PaymentSwitch.Data.Implementation;
using PaymentSwitch.Services.Abstraction;
using PaymentSwitch.Services.Implementation;
using PaymentSwitch.Utility;
using Polly;
using Polly.Extensions.Http;
using Swashbuckle.AspNetCore.Filters;
using System.Security.Authentication;
using System.Text;
using System.Threading.RateLimiting;
using static PaymentSwitch.Utility.AppEnums;

namespace PaymentSwitch.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
        {
            StaticData.MibssLogin = builder.Configuration[StaticData.LoginUrlFromAppsettings];

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection(StaticData.Con_Strings));
            builder.Services.Configure<BackgroundSettings>(builder.Configuration.GetSection(StaticData.BG_Settings));
            
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient(StaticData.PaymentIntegration)
                       .AddPolicyHandler(HttpPolicyExtensions
                       .HandleTransientHttpError()
                       .Or<AuthenticationException>() // Handle SSL errors
                       .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            builder.Services.AddScoped<INipClient, NipClient>();
            builder.Services.AddScoped<ITransferRepository, TransferRepository>();
            builder.Services.AddScoped<ITransferService, TransferService>();
            builder.Services.AddScoped<IDapperRepository, DapperRepository>();
            builder.Services.AddScoped<IAuthUser, AuthUser>();
            builder.Services.AddScoped<AdminUserFilter>();
            //builder.Services.AddScoped<IBaseService, BaseService>();       
            //builder.Services.AddScoped<IMibssServiceCalls, MibssServiceCalls>();      

            builder.Services.AddRateLimiter(ratelimiteroptions =>
            {
                ratelimiteroptions.AddFixedWindowLimiter("fixed", options =>
                {
                    options.PermitLimit = 5;
                    options.Window = TimeSpan.FromSeconds(10);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 2;
                });
                ratelimiteroptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });


            return builder;
        }

        public static WebApplicationBuilder AddJwtConfig(this WebApplicationBuilder builder)
        {

            var jwtOption = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
            bool hasExpired = false;
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ClockSkew = TimeSpan.Zero,
                        RequireSignedTokens = true,

                        ValidateLifetime = true,
                        RequireExpirationTime = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOption.Key)),

                        ValidateAudience = true,
                        ValidAudience = jwtOption.Audience,

                        ValidateIssuer = true,
                        ValidIssuer = jwtOption.Issuer,
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                hasExpired = true;
                                context.Response.Headers.Append("Token-Expired", "true");
                                context.Response.StatusCode = 406;
                                return Task.CompletedTask;
                            }
                            else
                            {
                                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                                logger.LogError(context.Exception, "Authentication Failed");
                                return Task.CompletedTask;
                            }
                        },
                        OnMessageReceived = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnChallenge = async (context) =>
                        {
                            if (context.AuthenticateFailure != null)
                            {
                                if (hasExpired)
                                {
                                    context.Response.StatusCode = 406;
                                    ApiResult apiResult = new()
                                    {
                                        Count = 0,
                                        HasError = false,
                                        Message = "Token Expired. Request Access Denied".ToLower(),
                                        StatusCode = StatusCodesEnum.TokenExpired
                                    };
                                    await context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(apiResult));
                                    return;
                                }
                                else
                                {
                                    context.Response.StatusCode = 401;
                                    ApiResult apiResult = new()
                                    {
                                        Count = 0,
                                        HasError = false,
                                        Message = "Token Validation Has Failed. Request Access Denied".ToLower(),
                                        StatusCode = StatusCodesEnum.NotAuthenticated
                                    };
                                    await context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(apiResult));
                                    return;
                                }
                                // we can decide to write our own custom response content here
                                //await context.HttpContext.SuccessResponse.WriteAsync($"{context.AuthenticateFailure}");
                            }
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                            logger.LogError(context.Error, $"OnChallenge Error{context.Error}::::{context.ErrorDescription}");
                            return;
                        },
                    };
                });


            builder.Services.AddApiVersioning(o =>
            {
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1.0);
                o.ReportApiVersions = true;
                o.ApiVersionReader = ApiVersionReader.Combine(
                   new QueryStringApiVersionReader(StaticData.ApiVersion),
                   new HeaderApiVersionReader(StaticData.X_Version));

            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            builder.Services.AddCors(o => o.AddPolicy(StaticData.PaymentIntegration, builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); }));

            builder.Services.AddSwaggerGen(option =>
            {
                // option.ExampleFilters();
                option.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, securityScheme: new OpenApiSecurityScheme
                {
                    Name = StaticData.Authorization,
                    Description = StaticData.SecuritySchemeDescription,
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = StaticData.Bearer
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                   {
                     new OpenApiSecurityScheme
                     {
                       Reference = new OpenApiReference
                       {
                        Type =ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                       }
                     }, new string[]{}
                   }
                });
            });


            return builder;
        }


    }
}
