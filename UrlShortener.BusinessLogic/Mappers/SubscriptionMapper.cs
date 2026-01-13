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