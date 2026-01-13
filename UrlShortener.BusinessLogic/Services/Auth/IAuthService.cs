using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Wrappers;

namespace UrlShortener.BusinessLogic.Services.Auth;

public interface IAuthService
{
    Task<ServiceResponse<AuthResultDto>> SignUpAsync(SignUpRequestDto dto, CancellationToken ct = default);
    Task<ServiceResponse<AuthResultDto>> LoginAsync(LoginRequestDto dto, CancellationToken ct = default);
}