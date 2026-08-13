using System.Security.Cryptography;
using System.Text;

namespace SecretSpots.Features.Auth;

// Shared by Register and ResetPassword — both need the exact same password strength rules,
// most importantly the common-password-hash check, which must stay in sync everywhere a
// password can be set.
internal static class PasswordRules
{
    public static bool ContainUpperCase(string password) => password.Any(char.IsUpper);

    public static bool ContainLowerCase(string password) => password.Any(char.IsLower);

    public static bool ContainDigit(string password) => password.Any(char.IsDigit);

    public static bool NotBeCommonPassword(string password)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password.ToLowerInvariant())))
            .ToLowerInvariant();
        return !CommonPasswordHashes.Values.Contains(hash);
    }
}
