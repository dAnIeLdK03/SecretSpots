namespace SecretSpots.Features.Common.Configuration;

public static class PaginationRequestExtensions
{
    public static (int Page, int PageSize) ResolvePaging(this PaginationOptionsBase options, int? page, int? pageSize) =>
        (page ?? 1, pageSize ?? options.DefaultPageSize);
}
