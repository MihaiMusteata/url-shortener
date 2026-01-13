namespace UrlShortener.BusinessLogic.Services.JwtToken;


public class JwtSettings
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 60;

    public string RsaPrivateKeyPem { get; set; } = "";
    public string RsaPublicKeyPem { get; set; } = "";
}
