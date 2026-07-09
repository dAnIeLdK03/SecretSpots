namespace SecretSpots.Features.Auth;

// Keys into the shared SharedResources.resx / SharedResources.bg.resx pair
// (Common/Localization) — Auth keeps only the constants, not the translations.
public static class AuthMessageKeys
{
    public const string EmailAlreadyRegistered = "Auth.EmailAlreadyRegistered";
    public const string InvalidCredentials = "Auth.InvalidCredentials";
    public const string InvalidOrExpiredRefreshToken = "Auth.InvalidOrExpiredRefreshToken";
    public const string EmailRequired = "Auth.EmailRequired";
    public const string EmailInvalidFormat = "Auth.EmailInvalidFormat";
    public const string PasswordRequired = "Auth.PasswordRequired";
    public const string PasswordTooShort = "Auth.PasswordTooShort";
    public const string DisplayNameRequired = "Auth.DisplayNameRequired";
    public const string DisplayNameTooLong = "Auth.DisplayNameTooLong";
    public const string RefreshTokenRequired = "Auth.RefreshTokenRequired";
    public const string PasswordRequiresUpper = "Auth.PasswordRequiresUpper";
    public const string PasswordRequiresLower = "Auth.PasswordRequiresLower";
    public const string PasswordRequiresDigit = "Auth.PasswordRequiresDigit";
    public const string PasswordRequiresSpecial = "Auth.PasswordRequiresSpecial";
    public const string PasswordIsCommon = "Auth.PasswordIsCommon";
    public const string UserNotFound = "Auth.UserNotFound";
}
