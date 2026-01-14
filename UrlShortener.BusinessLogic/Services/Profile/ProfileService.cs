using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProfileService> _logger;
    private readonly string _baseShortDomain;

    private static string CacheKey_Profile(Guid userId) => $"profile:me:{userId}";

    public ProfileService(
        IUserRepository users,
        ISubscriptionRepository subs,
        IShortLinkRepository shortLinks,
        IMemoryCache cache,
        ILogger<ProfileService> logger,
        IConfiguration cfg)
    {
        _users = users;
        _subs = subs;
        _shortLinks = shortLinks;
        _cache = cache;
        _logger = logger;
        _baseShortDomain = cfg["ShortLinks:BaseUrl"] ?? "http://localhost:5093";
    }

    public async Task<ServiceResponse<ProfilePageDto>> GetMyProfileAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetMyProfile failed: unauthorized (empty userId).");
            return ServiceResponse<ProfilePageDto>.Fail("Unauthorized.");
        }

        var cacheKey = CacheKey_Profile(userId);

        if (_cache.TryGetValue(cacheKey, out ProfilePageDto? cachedDto) && cachedDto is not null)
        {
            _logger.LogDebug("Profile loaded from cache. UserId={UserId}", userId);
            return ServiceResponse<ProfilePageDto>.Ok(cachedDto);
        }

        _logger.LogInformation("Loading profile from database. UserId={UserId}", userId);

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            _logger.LogWarning("GetMyProfile failed: user not found. UserId={UserId}", userId);
            return ServiceResponse<ProfilePageDto>.Fail("User not found.");
        }

        var activeSub = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (activeSub?.Plan is null)
        {
            _logger.LogWarning("GetMyProfile failed: no active plan. UserId={UserId}", userId);
            return ServiceResponse<ProfilePageDto>.Fail("No active plan.");
        }

        var plan = activeSub.Plan;

        var now = DateTime.UtcNow;
        var linksCreatedThisMonth =
            await _shortLinks.CountCreatedByUserInMonthAsync(
                userId,
                now.Year,
                now.Month,
                ct);

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

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(2))
            .SetSlidingExpiration(TimeSpan.FromMinutes(1));

        _cache.Set(cacheKey, dto, cacheOptions);

        _logger.LogInformation(
            "Profile cached. UserId={UserId}, Links={LinksCount}, LinksCreatedThisMonth={LinksCreatedThisMonth}, Plan={PlanName}",
            userId,
            dto.Links.Count,
            linksCreatedThisMonth,
            plan.Name);

        return ServiceResponse<ProfilePageDto>.Ok(dto);
    }

    private string BuildShortUrl(string code)
    {
        if (_baseShortDomain.EndsWith("/"))
            return _baseShortDomain + code;

        return _baseShortDomain + "/" + code;
    }

    public void InvalidateProfileCache(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogDebug("InvalidateProfileCache ignored: empty userId.");
            return;
        }

        _cache.Remove(CacheKey_Profile(userId));
        _logger.LogDebug("Profile cache invalidated. UserId={UserId}", userId);
    }
}
