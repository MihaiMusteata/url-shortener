using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.BusinessLogic.Mappers;

public static class ProfileMapper
{
    public static UserProfileDto ToUserProfileDto(this UserDbTable u, PlanDbTable? plan)
    {
        return new UserProfileDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Username = u.Username,
            Email = u.Email,
            PlanName = plan?.Name ?? ""
        };
    }

    public static PlanSummaryDto ToPlanSummaryDto(this PlanDbTable? p)
    {
        if (p is null)
            return new PlanSummaryDto();

        return new PlanSummaryDto
        {
            Name = p.Name,
            PriceMonthly = p.PriceMonthly,
            MaxLinksPerMonth = p.MaxLinksPerMonth,
            CustomAliasEnabled = p.CustomAliasEnabled,
            QrEnabled = p.QrEnabled
        };
    }

    public static ShortLinkDto ToShortLinkDto(this ShortLinkDbTable x, string baseUrl)
    {
        return new ShortLinkDto
        {
            Id = x.Id,
            OriginalUrl = x.OriginalUrl,
            Alias = x.ShortCode,
            ShortUrl = $"{baseUrl}/{x.ShortCode}",
            CreatedAt = x.CreatedAt,
            QrEnabled = x.QrCode != null,
            Clicks = x.TotalClicks
        };
    }
}