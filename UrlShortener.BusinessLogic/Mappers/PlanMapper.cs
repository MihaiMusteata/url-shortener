using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.BusinessLogic.Mappers;

public static class PlanMapper
{
    public static PlanDto ToDto(this PlanDbTable plan)
    {
        return new PlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            PriceMonthly = plan.PriceMonthly,
            MaxLinksPerMonth = plan.MaxLinksPerMonth,
            CustomAliasEnabled = plan.CustomAliasEnabled,
            QrEnabled = plan.QrEnabled
        };
    }

    public static PlanDbTable ToEntity(this PlanDto plan, PlanDbTable? existing = null)
    {
        if (existing == null)
            return new PlanDbTable
            {
                Name = plan.Name,
                PriceMonthly = plan.PriceMonthly,
                MaxLinksPerMonth = plan.MaxLinksPerMonth,
                CustomAliasEnabled = plan.CustomAliasEnabled,
                QrEnabled = plan.QrEnabled
            };

        existing.Name = plan.Name;
        existing.PriceMonthly = plan.PriceMonthly;
        existing.MaxLinksPerMonth = plan.MaxLinksPerMonth;
        existing.CustomAliasEnabled = plan.CustomAliasEnabled;
        existing.QrEnabled = plan.QrEnabled;
        return existing;

    }
}