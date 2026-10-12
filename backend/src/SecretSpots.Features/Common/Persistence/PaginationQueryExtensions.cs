using Microsoft.EntityFrameworkCore;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Common.Persistence;

public static class PaginationQueryExtensions
{
    // Query already projects to the final response shape and is translatable to SQL end to end.
    public static async Task<PagedResponse<TItem>> ToPagedResponseAsync<TItem>(
        this IQueryable<TItem> orderedProjectedQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await orderedProjectedQuery.CountAsync(cancellationToken);

        var items = await orderedProjectedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TItem>(items, page, pageSize, totalCount);
    }

    // Query must materialize entities first (e.g. a geography column's .Y/.X, or an array
    // indexer like PhotoUrls[0], can't be translated to SQL by the Npgsql provider), then gets
    // projected to the response shape in memory.
    public static async Task<PagedResponse<TItem>> ToPagedResponseAsync<TSource, TItem>(
        this IQueryable<TSource> orderedQuery,
        int page,
        int pageSize,
        Func<TSource, TItem> project,
        CancellationToken cancellationToken)
    {
        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        var entities = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.ConvertAll(e => project(e));

        return new PagedResponse<TItem>(items, page, pageSize, totalCount);
    }
}
