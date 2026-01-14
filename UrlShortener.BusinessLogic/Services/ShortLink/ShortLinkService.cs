using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ShortLinkService> _logger;

    private readonly string _baseShortDomain;

    private const string DirectReferrer = "Direct";

    private static string CacheKey_Details(Guid linkId) => $"shortlink:details:{linkId}";
    private static string CacheKey_Resolve(string alias) => $"shortlink:resolve:{alias.ToLowerInvariant()}";

    public ShortLinkService(
        IShortLinkRepository shortLinks,
        ISubscriptionRepository subs,
        IProfileService profileService,
        IMemoryCache cache,
        ILogger<ShortLinkService> logger,
        IConfiguration cfg)
    {
        _shortLinks = shortLinks;
        _subs = subs;
        _profileService = profileService;
        _cache = cache;
        _logger = logger;
        _baseShortDomain = cfg["ShortLinks:BaseUrl"] ?? "http://localhost:5093";
    }

    public async Task<ServiceResponse<ShortLinkCreateResponseDto>> CreateAsync(
        Guid userId,
        ShortLinkCreateRequestDto req,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Create short link failed: unauthorized (empty userId).");
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Unauthorized.");
        }

        if (!UrlUtils.TryNormalizeHttpUrl(req.Url, out var normalized))
        {
            _logger.LogWarning("Create short link failed: invalid URL. UserId={UserId}, Url={Url}", userId, req.Url);
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Invalid URL.");
        }

        _logger.LogInformation("Creating short link. UserId={UserId}, HasCustomAlias={HasCustomAlias}, EnableQr={EnableQr}",
            userId, !string.IsNullOrWhiteSpace(req.CustomAlias), req.EnableQr);

        var activeSub = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (activeSub?.Plan is null)
        {
            _logger.LogWarning("Create short link blocked: no active plan. UserId={UserId}", userId);
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Upgrade required: No active plan.");
        }

        var plan = activeSub.Plan;

        var now = DateTime.UtcNow;
        var createdCount = await _shortLinks.CountCreatedByUserInMonthAsync(userId, now.Year, now.Month, ct);
        if (createdCount >= plan.MaxLinksPerMonth)
        {
            _logger.LogWarning(
                "Create short link blocked: monthly limit reached. UserId={UserId}, Limit={Limit}, Created={Created}",
                userId, plan.MaxLinksPerMonth, createdCount);

            return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                $"Upgrade required: You reached the monthly limit ({plan.MaxLinksPerMonth} links/month).");
        }

        string shortCode;
        if (!string.IsNullOrWhiteSpace(req.CustomAlias))
        {
            if (!plan.CustomAliasEnabled)
            {
                _logger.LogWarning("Create short link blocked: custom alias not allowed by plan. UserId={UserId}", userId);
                return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                    "Upgrade required: Custom alias is not available on your plan.");
            }

            if (!UrlUtils.IsValidAlias(req.CustomAlias))
            {
                _logger.LogWarning("Create short link failed: invalid custom alias. UserId={UserId}, Alias={Alias}",
                    userId, req.CustomAlias);
                return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                    "Custom alias must be 3–32 chars (letters, numbers, - or _).");
            }

            shortCode = req.CustomAlias;
            var exists = await _shortLinks.ShortCodeExistsAsync(shortCode, ct);
            if (exists)
            {
                _logger.LogWarning("Create short link failed: custom alias taken. UserId={UserId}, Alias={Alias}",
                    userId, shortCode);
                return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Custom alias is already taken.");
            }
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
            {
                _logger.LogError("Create short link failed: could not generate unique alias. UserId={UserId}", userId);
                return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                    "Could not generate a unique alias. Try again.");
            }
        }

        var wantQr = req.EnableQr;
        if (wantQr && !plan.QrEnabled)
        {
            _logger.LogWarning("Create short link blocked: QR not allowed by plan. UserId={UserId}", userId);
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                "Upgrade required: QR codes are not available on your plan.");
        }

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
            _logger.LogError(ex, "Error creating short link. UserId={UserId}, Alias={Alias}", userId, shortCode);
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Error creating short link.");
        }

        // invalidate profile cache (usage + list)
        _profileService.InvalidateProfileCache(userId);
        _logger.LogDebug("Profile cache invalidated after creating short link. UserId={UserId}", userId);

        // prime resolve cache
        _cache.Set(
            CacheKey_Resolve(entity.ShortCode),
            entity.OriginalUrl,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2)));

        _logger.LogInformation("Short link created. UserId={UserId}, LinkId={LinkId}, Alias={Alias}, QrEnabled={QrEnabled}",
            userId, entity.Id, entity.ShortCode, wantQr);

        var resp = new ShortLinkCreateResponseDto
        {
            Alias = entity.ShortCode,
            ShortUrl = BuildShortUrl(entity.ShortCode),
            QrUrl = entity.QrCode?.FileUrl
        };

        return ServiceResponse<ShortLinkCreateResponseDto>.Ok(resp);
    }

    public async Task<ServiceResponse<ShortLinkDetailsDto>> GetDetailsAsync(
        Guid userId,
        Guid shortLinkId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetDetails failed: unauthorized (empty userId).");
            return ServiceResponse<ShortLinkDetailsDto>.Fail("Unauthorized.");
        }

        if (shortLinkId == Guid.Empty)
        {
            _logger.LogWarning("GetDetails failed: invalid link id (empty). UserId={UserId}", userId);
            return ServiceResponse<ShortLinkDetailsDto>.Fail("Invalid link id.");
        }

        var detailsKey = CacheKey_Details(shortLinkId);

        if (_cache.TryGetValue(detailsKey, out ShortLinkDetailsDto? cached) && cached is not null)
        {
            _logger.LogDebug("Short link details loaded from cache. UserId={UserId}, LinkId={LinkId}", userId, shortLinkId);
            return ServiceResponse<ShortLinkDetailsDto>.Ok(cached);
        }

        _logger.LogInformation("Loading short link details from database. UserId={UserId}, LinkId={LinkId}", userId, shortLinkId);

        var entity = await _shortLinks.GetByIdWithDetailsAsync(shortLinkId, ct);
        if (entity is null)
        {
            _logger.LogWarning("GetDetails failed: link not found. LinkId={LinkId}", shortLinkId);
            return ServiceResponse<ShortLinkDetailsDto>.Fail("Link not found.");
        }

        if (entity.UserId != userId)
        {
            _logger.LogWarning("GetDetails forbidden. UserId={UserId}, LinkId={LinkId}, OwnerUserId={OwnerUserId}",
                userId, shortLinkId, entity.UserId);
            return ServiceResponse<ShortLinkDetailsDto>.Fail("Forbidden.");
        }

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

        _logger.LogInformation(
            "Short link details cached. UserId={UserId}, LinkId={LinkId}, TotalClicks={TotalClicks}",
            userId, shortLinkId, dto.TotalClicks);

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
        {
            _logger.LogWarning("Resolve failed: empty alias.");
            return ServiceResponse<string>.Fail("Invalid alias.");
        }

        if (!UrlUtils.IsValidAlias(alias))
        {
            _logger.LogWarning("Resolve failed: invalid alias format. Alias={Alias}", alias);
            return ServiceResponse<string>.Fail("Invalid alias.");
        }

        var resolveKey = CacheKey_Resolve(alias);

        if (_cache.TryGetValue(resolveKey, out string? cachedOriginalUrl) && !string.IsNullOrWhiteSpace(cachedOriginalUrl))
        {
            _logger.LogDebug("Resolve cache hit. Alias={Alias}", alias);
            // NOTĂ: în implementarea ta tot încărcăm entity pentru tracking
        }
        else
        {
            _logger.LogDebug("Resolve cache miss. Alias={Alias}", alias);
        }

        var entity = await _shortLinks.GetByShortCodeAsync(alias, ct);
        if (entity is null)
        {
            _logger.LogWarning("Resolve failed: link not found. Alias={Alias}", alias);
            return ServiceResponse<string>.Fail("Link not found.");
        }

        if (!entity.IsActive)
        {
            _logger.LogWarning("Resolve blocked: link inactive. Alias={Alias}, LinkId={LinkId}", alias, entity.Id);
            return ServiceResponse<string>.Fail("Link inactive.");
        }

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
            _logger.LogError(ex, "Error tracking click. Alias={Alias}, LinkId={LinkId}", alias, entity.Id);
            return ServiceResponse<string>.Fail("Error tracking click.");
        }

        _cache.Set(
            resolveKey,
            entity.OriginalUrl,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2)));

        _cache.Remove(CacheKey_Details(entity.Id));
        _logger.LogDebug("Invalidated details cache after click. LinkId={LinkId}", entity.Id);

        _profileService.InvalidateProfileCache(entity.UserId);

        _logger.LogInformation("Resolve & track success. Alias={Alias}, LinkId={LinkId}, TotalClicks={TotalClicks}",
            alias, entity.Id, entity.TotalClicks);

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