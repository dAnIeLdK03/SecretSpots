using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Spots;

public static class SearchSpots
{
    // Relevance ranking (below) happens in memory, since it sums per-word matches across a
    // runtime word list — not something EF Core can translate into a SQL ORDER BY. That means
    // pagination can only be correct within whatever candidate set gets ranked, so this caps how
    // many matches are pulled into memory to be ranked and paged through. Generous for this
    // app's scale (a single country's hidden spots) — see TotalCount below for what happens if a
    // query ever matches more than this.
    private const int MaxCandidatesForRanking = 500;

    public record Query(string? SearchTerm, SpotCategory? Category, int Page, int PageSize)
        : IRequest<SpotSearchPageResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<SpotSearchOptions> spotSearchOptions)
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1).WithMessage(localizer[SpotsMessageKeys.PageOutOfRange].Value);

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, spotSearchOptions.Value.MaxPageSize)
                    .WithMessage(localizer[SpotsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, SpotSearchPageResponse>
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

        public async Task<SpotSearchPageResponse> Handle(Query query, CancellationToken cancellationToken)
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

            // A plain COUNT, independent of the in-memory ranking below — translates straight to
            // SQL and isn't limited by MaxCandidatesForRanking, so it reflects every actual match.
            var totalMatches = await matched.CountAsync(cancellationToken);

            var candidates = await matched.Take(MaxCandidatesForRanking).ToListAsync(cancellationToken);

            // Relevance ranking happens in memory over the (capped) candidate set — summing
            // per-word trigram scores across a runtime word list isn't something EF Core can
            // translate to SQL, and a simple "how many query words does this spot contain" count
            // is a good enough signal at this scale.
            var ordered = words.Length > 0
                ? candidates
                    .OrderByDescending(s => words.Count(w =>
                        s.Name.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                        s.Description.Contains(w, StringComparison.OrdinalIgnoreCase)))
                    .ThenByDescending(s => s.CreatedAt)
                : candidates.OrderByDescending(s => s.CreatedAt);

            var page = ordered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(s => new SpotSearchResultResponse(
                    s.Id,
                    s.Name,
                    s.Description,
                    s.Category,
                    s.PhotoUrls[0],
                    s.Location.Y,
                    s.Location.X,
                    s.CreatedByUserId,
                    s.CreatedAt))
                .ToList();

            // Clamped to what's actually reachable via pagination — reporting the true (uncapped)
            // totalMatches here would let the count claim more pages exist than candidates can
            // ever be ranked/paged through.
            var reportedTotalCount = Math.Min(totalMatches, MaxCandidatesForRanking);

            return new SpotSearchPageResponse(page, query.Page, query.PageSize, reportedTotalCount);
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
