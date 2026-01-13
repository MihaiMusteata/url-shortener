using Microsoft.Extensions.DependencyInjection;
using UrlShortener.BusinessLogic.Services.Plan;
using UrlShortener.DataAccess.Repositories.Plan;

namespace UrlShortener.BusinessLogic;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<IPlanService, PlanService>();
        return services;
    }
    
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IPlanRepository, PlanRepository>();

        return services;
    }
}
