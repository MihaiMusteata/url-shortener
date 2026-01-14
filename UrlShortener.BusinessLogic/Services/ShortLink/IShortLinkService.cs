using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.BusinessLogic.Services.ShortLink;

public interface IShortLinkService
{
    Task<ServiceResponse<ShortLinkCreateResponseDto>> CreateAsync(Guid userId, ShortLinkCreateRequestDto req, CancellationToken ct = default);
    Task<ServiceResponse<ShortLinkDetailsDto>> GetDetailsAsync(Guid userId, Guid shortLinkId, CancellationToken ct = default);
    Task<ServiceResponse<string>> ResolveAndTrackAsync(string alias, string? referrer, string? userAgent, string? ip, CancellationToken ct = default);
}