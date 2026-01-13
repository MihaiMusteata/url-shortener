using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Wrappers;

namespace UrlShortener.BusinessLogic.Services.ShortLink;

public interface IShortLinkService
{
    Task<ServiceResponse<ShortLinkCreateResponseDto>> CreateAsync(Guid userId, ShortLinkCreateRequestDto req, CancellationToken ct = default);
}