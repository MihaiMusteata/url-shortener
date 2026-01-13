using Microsoft.Extensions.Configuration;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Repositories.ShortLink;
using UrlShortener.DataAccess.Repositories.Subscription;
using UrlShortener.DataAccess.Repositories.User;

namespace UrlShortener.BusinessLogic.Services.Profile;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _users;
    private readonly ISubscriptionRepository _subs;
    private readonly IShortLinkRepository _shortLinks;
    private readonly string _baseShortDomain;

    public ProfileService(
        IUserRepository users,
        ISubscriptionRepository subs,
        IShortLinkRepository shortLinks,
        IConfiguration cfg)
    {
        _users = users;
        _subs = subs;
        _shortLinks = shortLinks;
        _baseShortDomain = cfg["ShortLinks:BaseUrl"] ?? "https://sho.rt";
    }

    public async Task<ServiceResponse<ProfilePageDto>> GetMyProfileAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<ProfilePageDto>.Fail("Unauthorized.");

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResponse<ProfilePageDto>.Fail("User not found.");

        var activeSub = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (activeSub?.Plan is null)
            return ServiceResponse<ProfilePageDto>.Fail("No active plan.");

        var plan = activeSub.Plan;

        var now = DateTime.UtcNow;
        var linksCreatedThisMonth = await _shortLinks.CountCreatedByUserInMonthAsync(userId, now.Year, now.Month, ct);

        var links = await _shortLinks.GetByUserIdAsync(userId, ct);

        var dto = new ProfilePageDto
        {
            User = new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username,
                Email = user.Email,
                PlanName = plan.Name
            },
            Plan = new PlanSummaryDto
            {
                Name = plan.Name,
                PriceMonthly = plan.PriceMonthly,
                MaxLinksPerMonth = plan.MaxLinksPerMonth,
                CustomAliasEnabled = plan.CustomAliasEnabled,
                QrEnabled = plan.QrEnabled
            },
            Usage = new UsageSummaryDto
            {
                LinksCreatedThisMonth = linksCreatedThisMonth
            },
            Links = links.Select(l => new ShortLinkDto
            {
                Id = l.Id,
                OriginalUrl = l.OriginalUrl,
                Alias = l.ShortCode,
                ShortUrl = BuildShortUrl(l.ShortCode),
                CreatedAt = l.CreatedAt,
                QrEnabled = l.QrCode != null,
                Clicks = l.TotalClicks
            }).ToList()
        };

        return ServiceResponse<ProfilePageDto>.Ok(dto);
    }

    private string BuildShortUrl(string code)
    {
        if (_baseShortDomain.EndsWith("/"))
            return _baseShortDomain + code;
        return _baseShortDomain + "/" + code;
    }
}