using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Admin;

public static class GetAdminUser
{
    public record Query(Guid UserId) : IRequest<Result<AdminUserDetailsResponse>>;

    public class Handler(IAppDbContext db, IStringLocalizer<SharedResources> localizer)
        : IRequestHandler<Query, Result<AdminUserDetailsResponse>>
    {
        public async Task<Result<AdminUserDetailsResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            // One round trip: every count is a correlated subquery on the same user row.
            var user = await db.Users
                .Where(u => u.Id == query.UserId)
                .Select(u => new AdminUserDetailsResponse(
                    u.Id,
                    u.Email,
                    u.DisplayName,
                    u.Role,
                    u.IsEmailVerified,
                    u.PasswordHash != null,
                    u.CreatedAt,
                    db.ExternalLogins.Where(l => l.UserId == u.Id).Select(l => l.Provider).ToList(),
                    new AdminUserActivityResponse(
                        db.Spots.Count(s => s.CreatedByUserId == u.Id && s.ModerationStatus == SpotModerationStatus.Pending),
                        db.Spots.Count(s => s.CreatedByUserId == u.Id && s.ModerationStatus == SpotModerationStatus.Approved),
                        db.Spots.Count(s => s.CreatedByUserId == u.Id && s.ModerationStatus == SpotModerationStatus.Rejected),
                        db.Comments.Count(c => c.UserId == u.Id),
                        db.CheckIns.Count(c => c.UserId == u.Id),
                        db.Ratings.Count(r => r.UserId == u.Id),
                        db.Reports.Count(r => r.ReporterUserId == u.Id),
                        db.CrystalWallets.Where(w => w.UserId == u.Id).Select(w => (int?)w.Balance).FirstOrDefault() ?? 0)))
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result<AdminUserDetailsResponse>.Failure(Error.NotFound(AdminMessageKeys.UserNotFound, localizer));
            }

            return Result<AdminUserDetailsResponse>.Success(user);
        }
    }
}
