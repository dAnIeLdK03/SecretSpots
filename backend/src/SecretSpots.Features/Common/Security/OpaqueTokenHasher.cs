using System.Security.Cryptography;
using System.Text;

namespace SecretSpots.Features.Common.Security;

// Refresh tokens and password-reset tokens are bearer secrets — anyone holding the raw value can
// use it directly (no password to crack, unlike PasswordHash). Only the hash goes to the
// database, so a database leak alone doesn't hand out usable tokens.
public static class OpaqueTokenHasher
{
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
