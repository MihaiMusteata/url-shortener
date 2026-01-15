using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using UrlShortener.BusinessLogic;
using UrlShortener.BusinessLogic.Context;
using UrlShortener.DataAccess;

var logger = LogManager
    .Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseNLog();

    builder.Services.AddControllers();

    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
    // builder.Services.Configure<ProfileService.PublicUrlsOptions>(builder.Configuration.GetSection("PublicUrls"));

    builder.Services.AddDataAccess();
    builder.Services.AddBusinessLogic();
    builder.Services.AddAuth(builder.Configuration);

    var corsOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policy =>
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseCors("CorsPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "API stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}