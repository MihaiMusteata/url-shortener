using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.BusinessLogic.Services.JwtToken;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly RsaSecurityKey _privateKey;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;

        if (string.IsNullOrWhiteSpace(_settings.RsaPrivateKeyPem))
            throw new InvalidOperationException("JWT RSA private key is missing.");

        var rsa = RSA.Create();
        rsa.ImportFromPem(_settings.RsaPrivateKeyPem.ToCharArray());
        _privateKey = new RsaSecurityKey(rsa);
    }

    public (string token, DateTime expiresAtUtc) Generate(UserDbTable user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("username", user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
