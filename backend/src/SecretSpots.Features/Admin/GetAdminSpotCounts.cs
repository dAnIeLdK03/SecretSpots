using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Admin;

public static class GetAdminSpotCounts
{
    public record Query : IRequest<AdminSpotCountsResponse>;

    public class Handler(IAppDbContext db) : IRequestHandler<Query, AdminSpotCountsResponse>
    {
        public async Task<AdminSpotCountsResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var counts = await db.Spots
                .GroupBy(s => s.ModerationStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

            return new AdminSpotCountsResponse(
                counts.GetValueOrDefault(SpotModerationStatus.Pending),
                counts.GetValueOrDefault(SpotModerationStatus.Approved),
                counts.GetValueOrDefault(SpotModerationStatus.Rejected));
        }
    }
}

public record AdminSpotCountsResponse(int Pending, int Approved, int Rejected);
