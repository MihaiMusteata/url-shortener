using Microsoft.Extensions.Logging;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Mappers;
using UrlShortener.BusinessLogic.Services.JwtToken;
using UrlShortener.BusinessLogic.Services.Password;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Entities;
using UrlShortener.DataAccess.Repositories.User;

namespace UrlShortener.BusinessLogic.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        ILogger<AuthService> logger)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<ServiceResponse<AuthResultDto>> SignUpAsync(
        SignUpRequestDto dto,
        CancellationToken ct = default)
    {
        _logger.LogInformation("SignUp attempt for email {Email}", dto.Email);

        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Username) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            _logger.LogWarning("SignUp failed: missing required fields");

            return ServiceResponse<AuthResultDto>
                .Fail("Email, username and password are required.");
        }

        if (dto.Password.Length < 6)
        {
            _logger.LogWarning("SignUp failed: password too short for email {Email}", dto.Email);

            return ServiceResponse<AuthResultDto>
                .Fail("Password must be at least 6 characters.");
        }

        var existingEmail = await _users.GetByEmailAsync(dto.Email, ct);
        if (existingEmail is not null)
        {
            _logger.LogWarning("SignUp failed: email already in use {Email}", dto.Email);

            return ServiceResponse<AuthResultDto>.Fail("Email is already in use.");
        }

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

        try
        {
            await _users.AddAsync(user, ct);
            await _users.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignUp failed: error creating user {Email}", dto.Email);

            return ServiceResponse<AuthResultDto>
                .Fail("Error creating account.");
        }

        var (token, exp) = _jwt.Generate(user);

        _logger.LogInformation("User created successfully. UserId={UserId}, Email={Email}", user.Id, user.Email);

        return ServiceResponse<AuthResultDto>.Ok(new AuthResultDto
        {
            AccessToken = token,
            ExpiresAtUtc = exp,
            User = user.ToMinimalDto()
        }, "Account created.");
    }

    public async Task<ServiceResponse<AuthResultDto>> LoginAsync(
        LoginRequestDto dto,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Login attempt for email {Email}", dto.Email);

        var email = dto.Email;
        var password = dto.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Login failed: missing credentials for email {Email}", email);

            return ServiceResponse<AuthResultDto>
                .Fail("Email and password are required.");
        }

        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null)
        {
            _logger.LogWarning("Login failed: user not found for email {Email}", email);

            return ServiceResponse<AuthResultDto>.Fail("Invalid credentials.");
        }

        if (!_hasher.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid password for userId {UserId}", user.Id);

            return ServiceResponse<AuthResultDto>.Fail("Invalid credentials.");
        }

        var (token, exp) = _jwt.Generate(user);

        _logger.LogInformation("Login successful. UserId={UserId}, Email={Email}", user.Id, user.Email);

        return ServiceResponse<AuthResultDto>.Ok(new AuthResultDto
        {
            AccessToken = token,
            ExpiresAtUtc = exp,
            User = user.ToMinimalDto()
        }, "Logged in.");
    }
}