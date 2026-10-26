namespace SecretSpots.Features.Admin;

// Shared by every admin action that takes a free-text reason the creator will see (RejectSpot,
// RejectSpotChangeRequest). The frontend mirrors MaxLength in lib/adminApi.ts.
public static class RejectionReasonRules
{
    public const int MaxLength = 500;
}
