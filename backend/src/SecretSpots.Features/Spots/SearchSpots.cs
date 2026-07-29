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
        // word_similarity()'s score distribution differs from plain similarity() — verified
        // against real seeded content that unrelated words share a "noise floor" up to ~0.3
        // (e.g. an unrelated description scored 0.23 against "waterfalls"), while genuine matches
        // (including a one-letter-swap typo, which scored 0.4) sit clearly above 0.35.
        private const double SimilarityThreshold = 0.35;

        // Multi-word "vibe" queries (e.g. a popular-search tag like "Планински гледки") rarely
        // appear verbatim anywhere — usually only ONE word of the phrase actually occurs in a
        // spot's name/description. Matching the whole phrase against a whole field (ILIKE or
        // trigram similarity) reliably misses these; splitting into words and matching per-word
        // is what actually finds them. Short function words are dropped so they don't dominate.
        private static readonly string[] StopWords = ["и", "в", "на", "от", "за", "с", "по", "the", "a", "an", "of", "in", "at"];

        public async Task<IReadOnlyList<SpotSearchResultResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseSpots = db.Spots.AsQueryable();

            if (query.Category is not null)
            {
                baseSpots = baseSpots.Where(s => s.Category == query.Category);
            }

            var words = SplitIntoWords(query.SearchTerm);

            var matched = baseSpots;
            if (words.Length > 0)
            {
                // Each word's sub-query is a plain, independently-translatable EF Core query (the
                // same shape as the original single-term version) — OR-ing across words happens
                // via SQL UNION instead of trying to loop a dynamic word list inside one Where
                // lambda, which EF Core/Npgsql can't reliably translate for ILIKE/trigram calls.
                // word_similarity(word, text) — not plain similarity() — finds the best-matching
                // substring of a long field for a short word, instead of comparing the whole
                // field as one blob (which dilutes a short word's score in a long description to
                // near-zero: verified 0.07 with similarity() vs 0.78 with word_similarity() for
                // the same "Водопади" vs. a waterfall-mentioning description).
                matched = words
                    .Select(word => baseSpots.Where(s =>
                        EF.Functions.ILike(s.Name, $"%{word}%") ||
                        EF.Functions.ILike(s.Description, $"%{word}%") ||
                        EF.Functions.TrigramsWordSimilarity(word, s.Name) > SimilarityThreshold ||
                        EF.Functions.TrigramsWordSimilarity(word, s.Description) > SimilarityThreshold))
                    .Aggregate((accumulated, next) => accumulated.Union(next));
            }

            var spots = await matched.Take(MaxResult).ToListAsync(cancellationToken);

            // Relevance ranking happens in memory after the (already capped) DB round trip —
            // summing per-word trigram scores across a runtime word list isn't something EF Core
            // can translate to SQL, and a simple "how many query words does this spot contain"
            // count is a good enough signal at this scale.
            var ordered = words.Length > 0
                ? spots
                    .OrderByDescending(s => words.Count(w =>
                        s.Name.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                        s.Description.Contains(w, StringComparison.OrdinalIgnoreCase)))
                    .ThenByDescending(s => s.CreatedAt)
                : spots.OrderByDescending(s => s.CreatedAt);

            return ordered.Select(s => new SpotSearchResultResponse(
                s.Id,
                s.Name,
                s.Description,
                s.Category,
                s.PhotoUrls[0],
                s.Location.Y,
                s.Location.X,
                s.CreatedByUserId,
                s.CreatedAt)).ToList();
        }

        private static string[] SplitIntoWords(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return [];
            }

            var words = searchTerm
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(w => w.Length >= 3 && !StopWords.Contains(w.ToLowerInvariant()))
                .Distinct()
                .ToArray();

            // Every word got filtered out as a stop word (e.g. a very short query) — fall back to
            // the raw trimmed term so we still search for something instead of matching nothing.
            return words.Length > 0 ? words : [searchTerm.Trim()];
        }
    }
}
