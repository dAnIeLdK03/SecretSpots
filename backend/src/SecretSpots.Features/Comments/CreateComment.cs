using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Notifications;
using WebPush;

namespace SecretSpots.Features.Comments;

public static class CreateComment
{
    public record RequestBody(string Text);

    public record Command(Guid SpotId, string Text) : IRequest<Result<CommentResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<CommentOptions> commentOptions)
        {
            RuleFor(c => c.Text)
                .NotEmpty().WithMessage(localizer[CommentsMessageKeys.TextRequired].Value)
                .MaximumLength(commentOptions.Value.MaxTextLength).WithMessage(localizer[CommentsMessageKeys.TextTooLong].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        WebPushClient webPushClient,
        IOptions<WebPushOptions> webPushOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<CommentResponse>>
    {
        public async Task<Result<CommentResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<CommentResponse>.Failure(new Error(
                    CommentsMessageKeys.SpotNotFound,
                    localizer[CommentsMessageKeys.SpotNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            // Same "current authenticated user" edge case as GetCurrentUser/CreateCheckIn —
            // handled the same way (graceful 404), not an unhandled exception.
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user is null)
            {
                return Result<CommentResponse>.Failure(new Error(
                    AuthMessageKeys.UserNotFound,
                    localizer[AuthMessageKeys.UserNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                SpotId = command.SpotId,
                UserId = userContext.UserId,
                Text = command.Text.Trim(),
            };

            db.Comments.Add(comment);

            // Skip notifying on a self-comment — a spot's own creator commenting on their own
            // spot shouldn't page themselves.
            Notification? notification = null;
            if (spot.CreatedByUserId != userContext.UserId)
            {
                notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = spot.CreatedByUserId,
                    Type = NotificationType.NewCommentOnYourSpot,
                    RelatedSpotId = spot.Id,
                };
                db.Notifications.Add(notification);
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(CommentsLogMessages.CommentCreated, comment.Id, comment.SpotId, user.Id);

            if (notification is not null)
            {
                await PushNotificationSender.SendAsync(
                    db, webPushClient, webPushOptions, localizer, logger, notification, cancellationToken);
            }

            return Result<CommentResponse>.Success(new CommentResponse(
                comment.Id,
                comment.SpotId,
                comment.UserId,
                user.DisplayName,
                comment.Text,
                comment.CreatedAt,
                comment.UpdatedAt));
        }
    }
}
