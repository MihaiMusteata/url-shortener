using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Wrappers;

namespace UrlShortener.BusinessLogic.Services.Profile;

public interface IProfileService
{
    Task<ServiceResponse<ProfilePageDto>> GetMyProfileAsync(Guid userId, CancellationToken ct = default);
}