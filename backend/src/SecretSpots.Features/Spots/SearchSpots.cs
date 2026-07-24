using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Spots;

public static class SearchSpots
{
    private const int MaxResult = 50;

    public record Query(string? SearchTerm, SpotCategory? Category)
        : IRequest<IReadOnlyList<SpotSearchResultResponse>>;

    public class Handler(IAppDbContext db) : IRequestHandler<Query, IReadOnlyList<SpotSearchResultResponse>>
    {
        private const double SimilarityThreshold = 0.2;
        public async Task<IReadOnlyList<SpotSearchResultResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var spots = db.Spots.AsQueryable();

            if (query.Category is not null)
            {
                spots = spots.Where(s => s.Category == query.Category);
            }

            var hasSearchTerm = !string.IsNullOrWhiteSpace(query.SearchTerm);
            var term = query.SearchTerm?.Trim() ?? "";
            var likePattern = $"%{term}%";

            if (hasSearchTerm)
            {
                spots = spots.Where(s => 
                    EF.Functions.ILike(s.Name, likePattern) ||
                    EF.Functions.ILike(s.Description, likePattern) ||
                    EF.Functions.TrigramsSimilarity(s.Name, term) > SimilarityThreshold ||
                    EF.Functions.TrigramsSimilarity(s.Description, term) > SimilarityThreshold);
            }

            var projected = spots.Select(s => new
            {
                Spot = s,
                Score = hasSearchTerm
                    ? EF.Functions.TrigramsSimilarity(s.Name, term) + EF.Functions.TrigramsSimilarity(s.Description, term)
                    : 0,
            });

            var ordered = hasSearchTerm
                ? projected.OrderByDescending(x => x.Score)
                : projected.OrderByDescending(x => x.Spot.CreatedAt);

            var result = await ordered.Take(MaxResult).ToListAsync(cancellationToken);

            return result.ConvertAll(s => new SpotSearchResultResponse(
                s.Spot.Id,
                s.Spot.Name,
                s.Spot.Description,
                s.Spot.Category,
                s.Spot.PhotoUrls[0],
                s.Spot.Location.Y,
                s.Spot.Location.X,
                s.Spot.CreatedByUserId,
                s.Spot.CreatedAt
            ));
        }
    }
}