namespace SecretSpots.Features.Common.Security;

// Internal guard-clause messages only (never shown to end users, never localized) —
// same rationale as Common/Results/ResultMessages.cs.
internal static class SecurityMessages
{
    public const string NoAuthenticatedUser = "IUserContext.UserId was read outside an authenticated request.";
}
