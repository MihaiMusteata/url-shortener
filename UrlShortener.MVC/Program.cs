using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using UrlShortener.BusinessLogic;
using UrlShortener.BusinessLogic.Services.Auth;
using UrlShortener.BusinessLogic.Services.JwtToken;
using UrlShortener.BusinessLogic.Services.Password;
using UrlShortener.DataAccess;

var logger = LogManager
    .Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseNLog();

    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpClient();

    builder.Services.AddBusinessLogic();
    builder.Services.AddDataAccess();

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

    builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
    builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";

            options.AccessDeniedPath = "/Error/403";

            options.SlidingExpiration = true;

            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.Redirect("/Error/403");
                    return Task.CompletedTask;
                },

                OnValidatePrincipal = async ctx =>
                {
                    if (!ctx.Request.Cookies.TryGetValue("access_token", out var jwt) ||
                        string.IsNullOrWhiteSpace(jwt))
                    {
                        ctx.RejectPrincipal();
                        await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return;
                    }

                    var jwtSettings = ctx.HttpContext.RequestServices
                        .GetRequiredService<IOptions<JwtSettings>>().Value;

                    if (string.IsNullOrWhiteSpace(jwtSettings.RsaPublicKeyPem))
                    {
                        ctx.RejectPrincipal();
                        await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return;
                    }

                    var rsa = RSA.Create();
                    rsa.ImportFromPem(jwtSettings.RsaPublicKeyPem.ToCharArray());
                    var rsaKey = new RsaSecurityKey(rsa);

                    var tokenValidationParams = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = rsaKey,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),

                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.NameIdentifier
                    };

                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var principal = handler.ValidateToken(jwt, tokenValidationParams, out _);

                        ctx.ReplacePrincipal(principal);
                        ctx.ShouldRenew = true;
                    }
                    catch
                    {
                        ctx.RejectPrincipal();
                        await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();
    app.UseForwardedHeaders();


    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
