using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Mappers;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Repositories.ShortLink;
using UrlShortener.DataAccess.Repositories.Subscription;
using UrlShortener.DataAccess.Repositories.User;

namespace UrlShortener.BusinessLogic.Services.Profile;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _users;
    private readonly ISubscriptionRepository _subs;
    private readonly IShortLinkRepository _links;
    private readonly PublicUrlsOptions _urls;

    public ProfileService(
        IUserRepository users,
        ISubscriptionRepository subs,
        IShortLinkRepository links,
        IOptions<PublicUrlsOptions> urls)
    {
        _users = users;
        _subs = subs;
        _links = links;
        _urls = urls.Value;
    }

    public async Task<ServiceResponse<ProfilePageDto>> GetMyProfileAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<ProfilePageDto>.Fail("Invalid user id.");

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResponse<ProfilePageDto>.Fail("User not found.");

        var activeSub = await _subs.GetActiveByUserIdAsync(userId, ct);
        var plan = activeSub?.Plan;

        var now = DateTime.UtcNow;
        var createdThisMonth = await _links.CountCreatedByUserInMonthAsync(userId, now.Year, now.Month, ct);

        var entities = await _links.GetByUserIdAsync(userId, ct);
        var linkDtos = entities
            .Select(x => x.ToShortLinkDto(_urls.ShortBaseUrl))
            .ToList();

        var dto = new ProfilePageDto
        {
            User = user.ToUserProfileDto(plan),
            Plan = plan.ToPlanSummaryDto(),
            Usage = new UsageSummaryDto { LinksCreatedThisMonth = createdThisMonth },
            Links = linkDtos
        };

        return ServiceResponse<ProfilePageDto>.Ok(dto);
    }
    
    
    public class PublicUrlsOptions
    {
        public string ShortBaseUrl { get; set; } = "https://sho.rt";
    }

}