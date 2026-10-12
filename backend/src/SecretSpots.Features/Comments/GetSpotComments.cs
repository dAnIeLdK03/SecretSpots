using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Comments;

public static class GetSpotComments
{
    public record Query(Guid SpotId, int Page, int PageSize) : IRequest<PagedResponse<CommentResponse>>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<CommentOptions> commentOptions)
        {
            this.ValidatePagination(
                q => q.Page,
                q => q.PageSize,
                commentOptions.Value.MaxPageSize,
                localizer[CommentsMessageKeys.PageOutOfRange].Value,
                localizer[CommentsMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, PagedResponse<CommentResponse>>
    {
        public async Task<PagedResponse<CommentResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery =
                from comment in db.Comments
                join user in db.Users on comment.UserId equals user.Id
                where comment.SpotId == query.SpotId && !comment.IsDeleted
                select new { comment, user.DisplayName };

            return await baseQuery
                .OrderByDescending(x => x.comment.CreatedAt)
                .Select(x => new CommentResponse(
                    x.comment.Id,
                    x.comment.SpotId,
                    x.comment.UserId,
                    x.DisplayName,
                    x.comment.Text,
                    x.comment.CreatedAt,
                    x.comment.UpdatedAt))
                .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);
        }
    }
}
