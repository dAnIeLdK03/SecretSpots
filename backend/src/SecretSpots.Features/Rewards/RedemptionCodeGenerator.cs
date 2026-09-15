using System.Security.Cryptography;

namespace SecretSpots.Features.Rewards;

// Short, spoken-aloud-friendly code shown to the customer and checked visually by business staff
// against the matching row in GetBusinessRedemptions — not a bearer secret (redeeming already
// happened; this only proves which row to mark fulfilled), so plain uppercase alphanumerics are
// fine. Excludes 0/O and 1/I/L, which are the pairs people misread most often off a phone screen.
internal static class RedemptionCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int Length = 8;

    public static string Generate()
    {
        Span<char> code = stackalloc char[Length];
        var bytes = RandomNumberGenerator.GetBytes(Length);

        for (var i = 0; i < Length; i++)
        {
            code[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(code);
    }
}
