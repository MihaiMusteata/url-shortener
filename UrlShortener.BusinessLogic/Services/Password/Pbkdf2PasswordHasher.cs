using System.Security.Cryptography;

namespace UrlShortener.BusinessLogic.Services.Password;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize
        );

        return $"PBKDF2$SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) return false;

        var parts = passwordHash.Split('$');
        if (parts.Length != 5) return false;
        if (!string.Equals(parts[0], "PBKDF2", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(parts[1], "SHA256", StringComparison.OrdinalIgnoreCase)) return false;

        if (!int.TryParse(parts[2], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[3]);
        var expected = Convert.FromBase64String(parts[4]);

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length
        );

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}