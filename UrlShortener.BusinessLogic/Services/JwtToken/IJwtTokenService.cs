using UrlShortener.DataAccess.Entities;

namespace UrlShortener.BusinessLogic.Services.JwtToken;

public interface IJwtTokenService
{
    (string token, DateTime expiresAtUtc) Generate(UserDbTable user);
}