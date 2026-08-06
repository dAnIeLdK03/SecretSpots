using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Comments;

public static class GetSpotComments
{
    public record Query(Guid SpotId, int Page, int PageSize) : IRequest<CommentsPageResponse>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<CommentOptions> commentOptions)
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1).WithMessage(localizer[CommentsMessageKeys.PageOutOfRange].Value);

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, commentOptions.Value.MaxPageSize)
                    .WithMessage(localizer[CommentsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, CommentsPageResponse>
    {
        public async Task<CommentsPageResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery =
                from comment in db.Comments
                join user in db.Users on comment.UserId equals user.Id
                where comment.SpotId == query.SpotId && !comment.IsDeleted
                select new { comment, user.DisplayName };

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(x => x.comment.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new CommentResponse(
                    x.comment.Id,
                    x.comment.SpotId,
                    x.comment.UserId,
                    x.DisplayName,
                    x.comment.Text,
                    x.comment.CreatedAt,
                    x.comment.UpdatedAt))
                .ToListAsync(cancellationToken);

            return new CommentsPageResponse(items, query.Page, query.PageSize, totalCount);
        }
    }
}
