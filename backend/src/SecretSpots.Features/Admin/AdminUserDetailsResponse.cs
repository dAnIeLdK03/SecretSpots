using SecretSpots.Domain;

namespace SecretSpots.Features.Admin;

public record AdminUserDetailsResponse(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsEmailVerified,
    bool HasPassword,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ExternalAuthProvider> LinkedProviders,
    AdminUserActivityResponse Activity);

public record AdminUserActivityResponse(
    int SpotsPending,
    int SpotsApproved,
    int SpotsRejected,
    int Comments,
    int CheckIns,
    int Ratings,
    int ReportsFiled,
    int CrystalBalance);
