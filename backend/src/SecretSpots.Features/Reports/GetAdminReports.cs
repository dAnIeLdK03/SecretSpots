using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Reports;

public static class GetAdminReports
{
    public record Query(int Page, int PageSize, bool IncludeResolved) : IRequest<AdminReportsPageResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<ReportsOptions> reportsOptions)
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1).WithMessage(localizer[ReportsMessageKeys.PageOutOfRange].Value);

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, reportsOptions.Value.MaxPageSize)
                    .WithMessage(localizer[ReportsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, AdminReportsPageResponse>
    {
        public async Task<AdminReportsPageResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery = db.Reports.AsQueryable();
            if (!query.IncludeResolved)
            {
                baseQuery = baseQuery.Where(r => r.ResolvedAt == null);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var reports = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // Report.ContentId is polymorphic (a Spot or a Comment id depending on ContentType),
            // not a real foreign key — so the preview text/spot link is resolved here in two
            // batched lookups instead of a join EF Core has no way to express generically.
            var spotIds = reports.Where(r => r.ContentType == ReportedContentType.Spot).Select(r => r.ContentId).ToList();
            var commentIds = reports.Where(r => r.ContentType == ReportedContentType.Comment).Select(r => r.ContentId).ToList();
            var reporterIds = reports.Select(r => r.ReporterUserId)
                .Concat(reports.Where(r => r.ResolvedByUserId.HasValue).Select(r => r.ResolvedByUserId!.Value))
                .Distinct()
                .ToList();

            var spots = await db.Spots
                .Where(s => spotIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

            var comments = await db.Comments
                .Where(c => commentIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Text, c.SpotId })
                .ToDictionaryAsync(c => c.Id, c => (c.Text, c.SpotId), cancellationToken);

            var users = await db.Users
                .Where(u => reporterIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

            var items = reports.Select(r =>
            {
                string? preview = null;
                Guid? relatedSpotId = null;

                if (r.ContentType == ReportedContentType.Spot)
                {
                    if (spots.TryGetValue(r.ContentId, out var name))
                    {
                        preview = name;
                        relatedSpotId = r.ContentId;
                    }
                }
                else if (comments.TryGetValue(r.ContentId, out var comment))
                {
                    preview = comment.Text;
                    relatedSpotId = comment.SpotId;
                }

                var reporterDisplayName = users.GetValueOrDefault(r.ReporterUserId, "?");
                var resolvedByDisplayName = r.ResolvedByUserId.HasValue
                    ? users.GetValueOrDefault(r.ResolvedByUserId.Value, "?")
                    : null;

                return new AdminReportResponse(
                    r.Id, r.ContentType, r.ContentId, relatedSpotId, preview,
                    reporterDisplayName, r.Reason, r.Details, r.CreatedAt, r.ResolvedAt,
                    resolvedByDisplayName, r.ResolutionAction);
            }).ToList();

            return new AdminReportsPageResponse(items, query.Page, query.PageSize, totalCount);
        }
    }
}
