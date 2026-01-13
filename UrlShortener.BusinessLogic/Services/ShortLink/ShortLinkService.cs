using Microsoft.Extensions.Configuration;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Helpers;
using UrlShortener.BusinessLogic.Services.ShortLink;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Entities;
using UrlShortener.DataAccess.Repositories.ShortLink;
using UrlShortener.DataAccess.Repositories.Subscription;

namespace UrlShortener.BusinessLogic.Services.ShortLink;

public class ShortLinkService : IShortLinkService
{
    private readonly IShortLinkRepository _shortLinks;
    private readonly ISubscriptionRepository _subs;

    private readonly string _baseShortDomain;

    public ShortLinkService(IShortLinkRepository shortLinks, ISubscriptionRepository subs, IConfiguration cfg)
    {
        _shortLinks = shortLinks;
        _subs = subs;
        _baseShortDomain = cfg["ShortLinks:BaseUrl"] ?? "https://sho.rt";
    }

    public async Task<ServiceResponse<ShortLinkCreateResponseDto>> CreateAsync(Guid userId,
        ShortLinkCreateRequestDto req, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Unauthorized.");

        if (!UrlUtils.TryNormalizeHttpUrl(req.Url, out var normalized))
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Invalid URL.");

        // plan (active subscription)
        var activeSub = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (activeSub?.Plan is null)
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail("Upgrade required: No active plan.");

        var plan = activeSub.Plan;

        // monthly limit
        var now = DateTime.UtcNow;
        var createdCount = await _shortLinks.CountCreatedByUserInMonthAsync(userId, now.Year, now.Month, ct);
        if (createdCount >= plan.MaxLinksPerMonth)
        {
            return ServiceResponse<ShortLinkCreateResponseDto>.Fail(
                $"Upgrade required: You reached the monthly limit ({plan.MaxLinksPerMonth} links/month).");
        }

        // permissions: custom alias
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
            // generate unique
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

        // permissions: QR
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

        var resp = new ShortLinkCreateResponseDto
        {
            Alias = entity.ShortCode,
            ShortUrl = BuildShortUrl(entity.ShortCode),
            QrUrl = entity.QrCode?.FileUrl
        };

        return ServiceResponse<ShortLinkCreateResponseDto>.Ok(resp);
    }

    private string BuildShortUrl(string code)
    {
        // safe join
        if (_baseShortDomain.EndsWith("/"))
            return _baseShortDomain + code;
        return _baseShortDomain + "/" + code;
    }
}