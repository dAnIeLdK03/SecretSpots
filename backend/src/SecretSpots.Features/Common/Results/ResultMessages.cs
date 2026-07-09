namespace SecretSpots.Features.Common.Results;

// Internal guard-clause messages only (never shown to end users, never localized —
// they signal a programming mistake, not a business or validation outcome).
internal static class ResultMessages
{
    public const string ValueAccessedOnFailure = "Cannot access the value of a failed result.";
    public const string ErrorAccessedOnSuccess = "Cannot access the error of a successful result.";
    public const string CannotConvertSuccessToProblem = "Cannot convert a successful result to a problem response.";
}
