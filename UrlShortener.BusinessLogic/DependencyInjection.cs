using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using UrlShortener.BusinessLogic.Services.Auth;
using UrlShortener.BusinessLogic.Services.JwtToken;
using UrlShortener.BusinessLogic.Services.Password;
using UrlShortener.BusinessLogic.Services.Plan;
using UrlShortener.BusinessLogic.Services.Profile;
using UrlShortener.BusinessLogic.Services.ShortLink;
using UrlShortener.BusinessLogic.Services.Subscription;
using UrlShortener.DataAccess.Repositories.Plan;
using UrlShortener.DataAccess.Repositories.ShortLink;
using UrlShortener.DataAccess.Repositories.Subscription;
using UrlShortener.DataAccess.Repositories.User;

namespace UrlShortener.BusinessLogic;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IShortLinkService, ShortLinkService>();
        services.AddMemoryCache();
        
        return services;
    }
    
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IShortLinkRepository, ShortLinkRepository>();
        
        return services;
    }
    
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtSettings>(config.GetSection("Jwt"));

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var settings = config.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

                if (string.IsNullOrWhiteSpace(settings.RsaPublicKeyPem))
                    throw new InvalidOperationException("JWT RSA public key is missing.");

                var rsa = RSA.Create();
                rsa.ImportFromPem(settings.RsaPublicKeyPem.ToCharArray());
                var rsaKey = new RsaSecurityKey(rsa);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = settings.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaKey,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();
        return services;
    }
}
