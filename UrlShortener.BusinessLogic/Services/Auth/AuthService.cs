using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Mappers;
using UrlShortener.BusinessLogic.Services.JwtToken;
using UrlShortener.BusinessLogic.Services.Password;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Entities;
using UrlShortener.DataAccess.Repositories.Subscription;
using UrlShortener.DataAccess.Repositories.User;

namespace UrlShortener.BusinessLogic.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<ServiceResponse<AuthResultDto>> SignUpAsync(SignUpRequestDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Username) ||
            string.IsNullOrWhiteSpace(dto.Password))
            return ServiceResponse<AuthResultDto>.Fail("Email, username and password are required.");

        if (dto.Password.Length < 6)
            return ServiceResponse<AuthResultDto>.Fail("Password must be at least 6 characters.");

        var existingEmail = await _users.GetByEmailAsync(dto.Email, ct);
        if (existingEmail is not null)
            return ServiceResponse<AuthResultDto>.Fail("Email is already in use.");
        
        var user = new UserDbTable
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Username = dto.Username,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = UserRole.user,
            PasswordHash = _hasher.Hash(dto.Password)
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        var (token, exp) = _jwt.Generate(user);

        return ServiceResponse<AuthResultDto>.Ok(new AuthResultDto
        {
            AccessToken = token,
            ExpiresAtUtc = exp,
            User = user.ToMinimalDto()
        }, "Account created.");
    }

    public async Task<ServiceResponse<AuthResultDto>> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        var email = dto.Email;
        var password = dto.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return ServiceResponse<AuthResultDto>.Fail("Email and password are required.");

        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null)
            return ServiceResponse<AuthResultDto>.Fail("Invalid credentials.");

        if (!_hasher.Verify(password, user.PasswordHash))
            return ServiceResponse<AuthResultDto>.Fail("Invalid credentials.");

        var (token, exp) = _jwt.Generate(user);

        return ServiceResponse<AuthResultDto>.Ok(new AuthResultDto
        {
            AccessToken = token,
            ExpiresAtUtc = exp,
            User = user.ToMinimalDto()
        }, "Logged in.");
    }
}
