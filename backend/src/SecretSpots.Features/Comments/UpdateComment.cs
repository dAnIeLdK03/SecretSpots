using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Comments;

public static class UpdateComment
{
    public record RequestBody(string Text);

    public record Command(Guid CommentId, string Text) : IRequest<Result<CommentResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<CommentOptions> commentOptions)
        {
            RuleFor(c => c.Text)
                .NotEmpty().WithMessage(localizer[CommentsMessageKeys.TextRequired].Value)
                .MaximumLength(commentOptions.Value.MaxTextLength).WithMessage(localizer[CommentsMessageKeys.TextTooLong].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<CommentResponse>>
    {
        public async Task<Result<CommentResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var comment = await db.Comments.SingleOrDefaultAsync(c => c.Id == command.CommentId && !c.IsDeleted, cancellationToken);
            if (comment is null)
            {
                return Result<CommentResponse>.Failure(Error.NotFound(CommentsMessageKeys.NotFound, localizer));
            }

            if (comment.UserId != userContext.UserId)
            {
                return Result<CommentResponse>.Failure(Error.Forbidden(CommentsMessageKeys.NotYourComment, localizer));
            }

            comment.Text = command.Text.Trim();
            comment.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            var authorDisplayName = await db.Users
                .Where(u => u.Id == comment.UserId)
                .Select(u => u.DisplayName)
                .SingleAsync(cancellationToken);

            logger.LogInformation(CommentsLogMessages.CommentUpdated, comment.Id, userContext.UserId);

            return Result<CommentResponse>.Success(new CommentResponse(
                comment.Id,
                comment.SpotId,
                comment.UserId,
                authorDisplayName,
                comment.Text,
                comment.CreatedAt,
                comment.UpdatedAt));
        }
    }
}
