using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Helpers;
using UrlShortener.BusinessLogic.Services.Profile;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Entities;
using UrlShortener.DataAccess.Repositories.ShortLink;
using UrlShortener.DataAccess.Repositories.Subscription;

namespace UrlShortener.BusinessLogic.Services.ShortLink;

public class ShortLinkService : IShortLinkService
{
    private readonly IShortLinkRepository _shortLinks;
    private readonly ISubscriptionRepository _subs;
    private readonly IProfileService _profileService;
    private readonly IMemoryCache _cache;

    private readonly string _baseShortDomain;

    private const string DirectReferrer = "Direct";

    private static string CacheKey_Details(Guid linkId) => $"shortlink:details:{linkId}";
    private static string CacheKey_Resolve(string alias) => $"shortlink:resolve:{alias.ToLowerInvariant()}";

    public ShortLinkService(
        IShortLinkRepository shortLinks,
        ISubscriptionRepository subs,
        IProfileService profileService,
        IMemoryCache cache,
        IConfiguration cfg)
    {
        _shortLinks = shortLinks;
        _subs = subs;
        _profileService = profileService;
        _cache = cache;
        _baseShortDomain = cfg["ShortLinks:BaseUrl"] ?? "http://localhost:5093";
    }

    public async Task<ServiceResponse<ShortLinkCreateResponseDto>> CreateAsync(
        Guid userId,
        ShortLinkCreateRequestDto req,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Unauthorized.");

        if (!UrlUtils.TryNormalizeHttpUrl(req.Url, out var normalized))
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Invalid URL.");

        var activeSub = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (activeSub?.Plan is null)
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Upgrade required: No active plan.");

        var plan = activeSub.Plan;

        var now = DateTime.UtcNow;
        var createdCount = await _shortLinks.CountCreatedByUserInMonthAsync(userId, now.Year, now.Month, ct);
        if (createdCount >= plan.MaxLinksPerMonth)
        {
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                $"Upgrade required: You reached the monthly limit ({plan.MaxLinksPerMonth} links/month).");
        }

        string shortCode;
        if (!string.IsNullOrWhiteSpace(req.CustomAlias))
        {
            if (!plan.CustomAliasEnabled)
                return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                    "Upgrade required: Custom alias is not available on your plan.");

            if (!UrlUtils.IsValidAlias(req.CustomAlias))
                return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                    "Custom alias must be 3–32 chars (letters, numbers, - or _).");

            shortCode = req.CustomAlias;
            var exists = await _shortLinks.ShortCodeExistsAsync(shortCode, ct);
            if (exists)
                return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Custom alias is already taken.");
        }
        else
        {
            shortCode = ShortCodeGenerator.Generate(7);
            for (var i = 0; i < 10; i++)
            {
                if (!await _shortLinks.ShortCodeExistsAsync(shortCode, ct)) break;
                shortCode = ShortCodeGenerator.Generate(7);
            }

            if (await _shortLinks.ShortCodeExistsAsync(shortCode, ct))
                return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                    "Could not generate a unique alias. Try again.");
        }

        var wantQr = req.EnableQr;
        if (wantQr && !plan.QrEnabled)
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                "Upgrade required: QR codes are not available on your plan.");

        var entity = new ShortLinkDbTable
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalUrl = normalized,
            ShortCode = shortCode,
            IsActive = true,
            TotalClicks = 0,
            CreatedAt = DateTime.UtcNow
        };

        if (wantQr)
        {
            var shortUrl = BuildShortUrl(shortCode);
            entity.QrCode = new QrCodeDbTable
            {
                Id = Guid.NewGuid(),
                ShortLinkId = entity.Id,
                Format = "png",
                FileUrl =
                    $"https://api.qrserver.com/v1/create-qr-code/?size=220x220&data={Uri.EscapeDataString(shortUrl)}"
            };
        }

        try
        {
            await _shortLinks.AddAsync(entity, ct);
            await _shortLinks.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail($"Error creating short link: {ex.Message}");
        }

        _profileService.InvalidateProfileCache(userId);

        _cache.Set(
            CacheKey_Resolve(entity.ShortCode),
            entity.OriginalUrl,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2)));

        var resp = new ShortLinkCreateResponseDto
        {
            Alias = entity.ShortCode,
            ShortUrl = BuildShortUrl(entity.ShortCode),
            QrUrl = entity.QrCode?.FileUrl
        };

        return ServiceResponse<ShortLinkCreateResponseDto>.Ok(resp);
    }

    public async Task<ServiceResponse<ShortLinkDetailsDto>> GetDetailsAsync(Guid userId, Guid shortLinkId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<ShortLinkDetailsDto>.Fail("Unauthorized.");

        if (shortLinkId == Guid.Empty)
            return ServiceResponse<ShortLinkDetailsDto>.Fail("Invalid link id.");

        var detailsKey = CacheKey_Details(shortLinkId);

        if (_cache.TryGetValue(detailsKey, out ShortLinkDetailsDto? cached) && cached is not null)
        {
            return ServiceResponse<ShortLinkDetailsDto>.Ok(cached);
        }

        var entity = await _shortLinks.GetByIdWithDetailsAsync(shortLinkId, ct);
        if (entity is null)
            return ServiceResponse<ShortLinkDetailsDto>.Fail("Link not found.");

        if (entity.UserId != userId)
            return ServiceResponse<ShortLinkDetailsDto>.Fail("Forbidden.");

        var todayUtc = DateTime.UtcNow.Date;
        var fromUtc = todayUtc.AddDays(-6);

        var clicksByDate = entity.LinkClicks
            .Where(c => c.ClickedAt >= fromUtc && c.ClickedAt < todayUtc.AddDays(1))
            .GroupBy(c => c.ClickedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var clicksLast7 = new List<DailyClicksDto>(7);
        for (var i = 0; i < 7; i++)
        {
            var day = fromUtc.AddDays(i);
            clicksByDate.TryGetValue(day, out var count);
            clicksLast7.Add(new DailyClicksDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                Count = count
            });
        }

        var normalizedReferrers = entity.LinkClicks
            .Select(c => NormalizeReferrer(c.Referer))
            .ToList();

        var uniqueReferrers = normalizedReferrers
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var topReferrers = normalizedReferrers
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TopReferrerDto { Referrer = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var recent = entity.LinkClicks
            .OrderByDescending(x => x.ClickedAt)
            .Take(100)
            .Select(x => new LinkClickEventDto
            {
                Id = x.Id,
                ClickedAt = x.ClickedAt,
                Referrer = NormalizeReferrer(x.Referer),
                Ua = SummarizeUserAgent(x.UserAgent)
            })
            .ToList();

        var shortUrl = BuildShortUrl(entity.ShortCode);

        var dto = new ShortLinkDetailsDto
        {
            Id = entity.Id,
            Alias = entity.ShortCode,
            ShortUrl = shortUrl,
            OriginalUrl = entity.OriginalUrl,
            CreatedAt = entity.CreatedAt,
            QrEnabled = entity.QrCode is not null,
            QrUrl = entity.QrCode?.FileUrl,

            TotalClicks = entity.TotalClicks,
            UniqueReferrers = uniqueReferrers,

            ClicksLast7Days = clicksLast7,
            TopReferrers = topReferrers,
            RecentEvents = recent
        };

        _cache.Set(
            detailsKey,
            dto,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));

        return ServiceResponse<ShortLinkDetailsDto>.Ok(dto);
    }

    public async Task<ServiceResponse<string>> ResolveAndTrackAsync(
        string alias,
        string? referrer,
        string? userAgent,
        string? ip,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return ServiceResponse<string>.Fail("Invalid alias.");

        if (!UrlUtils.IsValidAlias(alias))
            return ServiceResponse<string>.Fail("Invalid alias.");

        var resolveKey = CacheKey_Resolve(alias);

        _ = _cache.TryGetValue(resolveKey, out string? cachedOriginalUrl);

        var entity = await _shortLinks.GetByShortCodeAsync(alias, ct);
        if (entity is null)
            return ServiceResponse<string>.Fail("Link not found.");

        if (!entity.IsActive)
            return ServiceResponse<string>.Fail("Link inactive.");

        entity.TotalClicks += 1;

        var click = new LinkClickDbTable
        {
            ShortLinkId = entity.Id,
            ClickedAt = DateTime.UtcNow,
            Referer = referrer ?? "",
            UserAgent = userAgent ?? "",
        };

        entity.LinkClicks.Add(click);

        try
        {
            await _shortLinks.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            return ServiceResponse<string>.Fail($"Error tracking click: {ex.Message}");
        }

        _cache.Set(
            resolveKey,
            entity.OriginalUrl,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2)));

        _cache.Remove(CacheKey_Details(entity.Id));

        _profileService.InvalidateProfileCache(entity.UserId);

        return ServiceResponse<string>.Ok(entity.OriginalUrl);
    }

    private static string NormalizeReferrer(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return DirectReferrer;

        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            if (!string.IsNullOrEmpty(uri.Host))
                return uri.Host;
        }

        var s = raw;
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            s = s.Substring("http://".Length);
        else if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            s = s.Substring("https://".Length);

        var slashIdx = s.IndexOf('/');
        if (slashIdx >= 0) s = s.Substring(0, slashIdx);

        if (string.IsNullOrEmpty(s))
            return DirectReferrer;

        return s;
    }

    private static string SummarizeUserAgent(string? ua)
    {
        if (string.IsNullOrEmpty(ua))
            return "Unknown";

        var u = ua;

        string browser =
            u.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Edge" :
            u.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) &&
            !u.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Chrome" :
            u.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ? "Firefox" :
            u.Contains("Safari/", StringComparison.OrdinalIgnoreCase) &&
            !u.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ? "Safari" :
            "Other";

        string os =
            u.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows" :
            u.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase) ? "macOS" :
            u.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android" :
            u.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("iPad", StringComparison.OrdinalIgnoreCase) ? "iOS" :
            u.Contains("Linux", StringComparison.OrdinalIgnoreCase) ? "Linux" :
            "Other";

        var mobile = u.Contains("Mobile", StringComparison.OrdinalIgnoreCase) || os is "Android" or "iOS";

        return mobile ? $"{browser} • {os} • Mobile" : $"{browser} • {os}";
    }

    private string BuildShortUrl(string code)
    {
        if (_baseShortDomain.EndsWith("/"))
            return _baseShortDomain + code;
        return _baseShortDomain + "/" + code;
    }
}