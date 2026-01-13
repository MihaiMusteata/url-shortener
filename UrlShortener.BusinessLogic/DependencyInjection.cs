using Microsoft.Extensions.DependencyInjection;
using UrlShortener.BusinessLogic.Services.Plan;
using UrlShortener.BusinessLogic.Services.Subscription;
using UrlShortener.DataAccess.Repositories.Plan;
using UrlShortener.DataAccess.Repositories.Subscription;
using UrlShortener.DataAccess.Repositories.User;

namespace UrlShortener.BusinessLogic;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        return services;
    }
    
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        
        return services;
    }
}
