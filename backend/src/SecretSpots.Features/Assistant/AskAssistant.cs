using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Assistant;

// One question to the spot discovery assistant. The client keeps the conversation and sends recent
// turns back as plain text; nothing it sends can pose as a tool result or a system instruction.
public static partial class AskAssistant
{
    public const string UserRole = "user";
    public const string AssistantRole = "assistant";

    public record HistoryMessage(string Role, string Text);

    public record Command(
        string Question,
        IReadOnlyList<HistoryMessage>? History,
        string Locale,
        double? Latitude,
        double? Longitude) : IRequest<Result<AssistantAnswerResponse>>;

    private static readonly Dictionary<string, string> LanguageNames = new() { ["bg"] = "Bulgarian", ["en"] = "English" };

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<AssistantOptions> assistantOptions)
        {
            var options = assistantOptions.Value;

            RuleFor(c => c.Question)
                .NotEmpty().WithMessage(localizer[AssistantMessageKeys.QuestionRequired].Value)
                .MaximumLength(options.MaxQuestionLength).WithMessage(localizer[AssistantMessageKeys.QuestionTooLong].Value);

            RuleFor(c => c.History)
                .Must(history => history is null || history.Count <= options.MaxHistoryMessages)
                .WithMessage(localizer[AssistantMessageKeys.HistoryTooLong].Value);

            // Only the two conversational roles, as text: anything else ("system", "tool", ...) is the
            // client trying to put words in someone else's mouth.
            RuleForEach(c => c.History)
                .Must(m => m is not null
                    && (m.Role == UserRole || m.Role == AssistantRole)
                    && !string.IsNullOrWhiteSpace(m.Text)
                    && m.Text.Length <= options.MaxHistoryMessageLength)
                .WithMessage(localizer[AssistantMessageKeys.HistoryInvalid].Value);

            RuleFor(c => c.Locale)
                .Must(locale => locale is not null && LanguageNames.ContainsKey(locale))
                .WithMessage(localizer[AssistantMessageKeys.LocaleInvalid].Value);

            RuleFor(c => c)
                .Must(c => (c.Latitude is null && c.Longitude is null)
                    || (c.Latitude is >= -90 and <= 90 && c.Longitude is >= -180 and <= 180))
                .WithName(nameof(Command.Latitude))
                .WithMessage(localizer[AssistantMessageKeys.LocationInvalid].Value);
        }
    }

    // [spot:<id>] markers the system prompt asks the model to put after each recommended spot. Small
    // models sometimes shorten the id, so anything id-like is matched and resolved below.
    [GeneratedRegex(@"\s*\[spot:\s*([0-9a-fA-F-]+)\s*\]")]
    private static partial Regex SpotMarker();

    // A shortened id is only trusted if it still pins down a single spot.
    private const int MinIdPrefixLength = 8;

    public class Handler(
        [FromKeyedServices(AssistantChatClient.ServiceKey)] IChatClient chatClient,
        IAppDbContext db,
        IUserContext userContext,
        AssistantTools tools,
        IOptions<AssistantOptions> assistantOptions,
        TimeProvider timeProvider,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<AssistantAnswerResponse>>
    {
        public async Task<Result<AssistantAnswerResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var options = assistantOptions.Value;
            if (!options.Enabled)
            {
                return Result<AssistantAnswerResponse>.Failure(Error.NotFound(AssistantMessageKeys.Disabled, localizer));
            }

            var userId = userContext.UserId;
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var outcome = await AssistantUsageLimiter.TryConsumeAsync(
                db, userId, today, options.DailyQuestionsPerUser, options.DailyQuestionsTotal, cancellationToken);
            if (outcome != AssistantUsageLimiter.Outcome.Allowed)
            {
                var key = outcome == AssistantUsageLimiter.Outcome.UserLimitReached
                    ? AssistantMessageKeys.DailyLimitReached
                    : AssistantMessageKeys.ServiceLimitReached;
                return Result<AssistantAnswerResponse>.Failure(Error.TooManyRequests(key, localizer));
            }

            var toolset = new AssistantToolset(tools, options.MaxToolCallsPerQuestion);
            var chatOptions = new ChatOptions { Tools = toolset.Tools, MaxOutputTokens = options.MaxOutputTokens };
            var stopwatch = Stopwatch.StartNew();

            ChatResponse response;
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
                try
                {
                    response = await chatClient.GetResponseAsync(BuildMessages(command), chatOptions, timeout.Token);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Covers the provider being down, a timeout and a model that errors out: the user
                    // gets a plain "try again" and keeps the question.
                    logger.LogWarning(ex, AssistantLogMessages.ModelFailed, userId, stopwatch.ElapsedMilliseconds);
                    await AssistantUsageLimiter.RefundAsync(db, userId, today, CancellationToken.None);
                    return Result<AssistantAnswerResponse>.Failure(Error.ServiceUnavailable(AssistantMessageKeys.Unavailable, localizer));
                }
            }

            var text = response.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning(AssistantLogMessages.EmptyAnswer, userId, toolset.ToolCalls);
                await AssistantUsageLimiter.RefundAsync(db, userId, today, CancellationToken.None);
                return Result<AssistantAnswerResponse>.Failure(Error.ServiceUnavailable(AssistantMessageKeys.NoAnswer, localizer));
            }

            var spotIds = await VisibleCitedSpotsAsync(text, toolset.ReturnedSpotIds, cancellationToken);
            var answer = SpotMarker().Replace(text, string.Empty).Trim();

            logger.LogInformation(
                AssistantLogMessages.Answered,
                userId,
                stopwatch.ElapsedMilliseconds,
                toolset.ToolCalls,
                spotIds.Count,
                response.Usage?.InputTokenCount,
                response.Usage?.OutputTokenCount);

            return Result<AssistantAnswerResponse>.Success(new AssistantAnswerResponse(answer, spotIds));
        }

        // The system prompt stays fixed; the language, the location and the question travel together
        // in the last user message.
        private static List<ChatMessage> BuildMessages(Command command)
        {
            var messages = new List<ChatMessage> { new(ChatRole.System, AssistantSystemPrompt.Text) };
            foreach (var message in command.History ?? [])
            {
                messages.Add(new ChatMessage(message.Role == AssistantRole ? ChatRole.Assistant : ChatRole.User, message.Text));
            }

            var location = command.Latitude is { } latitude && command.Longitude is { } longitude
                ? string.Create(CultureInfo.InvariantCulture, $"{latitude:0.####}, {longitude:0.####} (from the user's device)")
                : "not shared";
            messages.Add(new ChatMessage(ChatRole.User,
            [
                new TextContent($"Context — language: {LanguageNames[command.Locale]}; user location: {location}."),
                new TextContent(command.Question.Trim()),
            ]));
            return messages;
        }

        // Keeps the ids the answer cites, in order, if they're real visible Approved spots: returned by
        // a tool for this question, or (for a spot from an earlier turn) confirmed in the database.
        // Anything else — a made-up id, or one smuggled in through the history — is dropped.
        private async Task<IReadOnlyList<Guid>> VisibleCitedSpotsAsync(
            string text, IReadOnlySet<Guid> returnedByTools, CancellationToken cancellationToken)
        {
            var cited = SpotMarker().Matches(text)
                .Select(m => Resolve(m.Groups[1].Value, returnedByTools))
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            var toConfirm = cited.Where(id => !returnedByTools.Contains(id)).ToList();
            var confirmed = toConfirm.Count == 0
                ? []
                : await db.Spots
                    .Where(s => toConfirm.Contains(s.Id) && s.ModerationStatus == SpotModerationStatus.Approved)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

            return cited.Where(id => returnedByTools.Contains(id) || confirmed.Contains(id)).ToList();
        }

        // A full id as is; a shortened one (e.g. "f144fa72") only if it's the start of exactly one id
        // the tools returned for this question.
        private static Guid Resolve(string cited, IReadOnlySet<Guid> returnedByTools)
        {
            if (Guid.TryParse(cited, out var id))
            {
                return id;
            }

            if (cited.Length < MinIdPrefixLength)
            {
                return Guid.Empty;
            }

            var matches = returnedByTools.Where(spotId => spotId.ToString().StartsWith(cited, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            return matches.Count == 1 ? matches[0] : Guid.Empty;
        }
    }
}
