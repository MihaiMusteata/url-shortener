using System.Text;

namespace UrlShortener.BusinessLogic.Helpers;

public static class ShortCodeGenerator
{
    private const string Alphabet = "abcdefghijkmnpqrstuvwxyz23456789";

    public static string Generate(int length = 7)
    {
        var rng = Random.Shared;
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append(Alphabet[rng.Next(Alphabet.Length)]);
        return sb.ToString();
    }
}