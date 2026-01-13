namespace UrlShortener.BusinessLogic.DTOs;

public class SignUpRequestDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Username { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public class LoginRequestDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResultDto
{
    public string AccessToken { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public UserMinimalDto User { get; set; } = new();
}