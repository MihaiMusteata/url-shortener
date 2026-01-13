using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.BusinessLogic.Mappers;

public static class SubscriptionMapper
{
    public static SubscriptionDto ToDto(this SubscriptionDbTable sub)
    {
        return new SubscriptionDto
        {
            Id = sub.Id,
            UserId = sub.UserId,
            PlanId = sub.PlanId,
            Active = sub.Active
        };
    }

    public static SubscriptionDetailsDto ToDetailsDto(this SubscriptionDbTable sub)
    {
        return new SubscriptionDetailsDto
        {
            Id = sub.Id,
            PlanId = sub.PlanId,
            Active = sub.Active,
            PlanName = sub.Plan?.Name,
            User = sub.User is null
                ? null
                : new UserMinimalDto
                {
                    Id = sub.User.Id,
                    FirstName = sub.User.FirstName,
                    LastName = sub.User.LastName,
                    Username = sub.User.Username,
                    Email = sub.User.Email
                }
        };
    }

    public static SubscriptionDbTable ToEntity(this SubscriptionDto sub)
    {
        return new SubscriptionDbTable
        {
            UserId = sub.UserId,
            PlanId = sub.PlanId,
            Active = sub.Active
        };
    }
}