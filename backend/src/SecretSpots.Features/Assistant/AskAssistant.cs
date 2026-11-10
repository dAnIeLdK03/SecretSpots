using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
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

// One question to the spot discovery assistant, answered as a stream of AssistantEvents (progress,
// then the text piece by piece, then the result). The client keeps the conversation and sends recent
// turns back as plain text; nothing it sends can pose as a tool result or a system instruction.
// Disabled and over-the-limit are ordinary failures returned before any streaming starts.
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
        double? Longitude) : IRequest<Result<IAsyncEnumerable<AssistantEvent>>>;

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
        : IRequestHandler<Command, Result<IAsyncEnumerable<AssistantEvent>>>
    {
        public async Task<Result<IAsyncEnumerable<AssistantEvent>>> Handle(Command command, CancellationToken cancellationToken)
        {
            var options = assistantOptions.Value;
            if (!options.Enabled)
            {
                return Result<IAsyncEnumerable<AssistantEvent>>.Failure(Error.NotFound(AssistantMessageKeys.Disabled, localizer));
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
                return Result<IAsyncEnumerable<AssistantEvent>>.Failure(Error.TooManyRequests(key, localizer));
            }

            return Result<IAsyncEnumerable<AssistantEvent>>.Success(AnswerAsync(command, userId, today, CancellationToken.None));
        }

        // Cancelling the enumeration (the user pressed Stop / closed the page) stops the model right
        // away. A stopped question still counts — the model already did the work — unlike one the
        // model failed to answer, which is given back.
        private async IAsyncEnumerable<AssistantEvent> AnswerAsync(
            Command command, Guid userId, DateOnly today, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var options = assistantOptions.Value;
            var toolset = new AssistantToolset(tools, options.MaxToolCallsPerQuestion);
            var chatOptions = new ChatOptions { Tools = toolset.Tools, MaxOutputTokens = options.MaxOutputTokens };
            AssistantChatClient.ApplyProviderOptions(chatOptions, options);
            var markers = new SpotMarkerStreamFilter();
            var text = new StringBuilder();
            long inputTokens = 0, outputTokens = 0;
            var stopwatch = Stopwatch.StartNew();

            yield return AssistantEvent.Status(localizer[AssistantMessageKeys.StatusThinking].Value);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
            var updates = chatClient.GetStreamingResponseAsync(BuildMessages(command, options.HistoryMessagesToModel), chatOptions, timeout.Token);
            await using var enumerator = updates.GetAsyncEnumerator(timeout.Token);

            Exception? failure = null;
            while (true)
            {
                // `yield` can't sit inside a try/catch, so only the step that talks to the model is caught.
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // The provider being down, the timeout, a model that errors out.
                    failure = ex;
                    break;
                }

                var update = enumerator.Current;
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        // Sent when the model asks for a tool, i.e. before the tool runs.
                        case FunctionCallContent call:
                            yield return AssistantEvent.Status(AssistantStatusMessages.For(call, toolset, localizer));
                            break;
                        case UsageContent usage:
                            inputTokens += usage.Details.InputTokenCount ?? 0;
                            outputTokens += usage.Details.OutputTokenCount ?? 0;
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(update.Text))
                {
                    text.Append(update.Text);
                    if (markers.Push(update.Text) is { Length: > 0 } visible)
                    {
                        yield return AssistantEvent.Delta(visible);
                    }
                }
            }

            if (failure is not null)
            {
                logger.LogWarning(failure, AssistantLogMessages.ModelFailed, userId, stopwatch.ElapsedMilliseconds);
                await AssistantUsageLimiter.RefundAsync(db, userId, today, CancellationToken.None);
                yield return AssistantEvent.Error(AssistantMessageKeys.Unavailable, localizer[AssistantMessageKeys.Unavailable].Value);
                yield break;
            }

            if (markers.Flush() is { Length: > 0 } rest)
            {
                yield return AssistantEvent.Delta(rest);
            }

            var fullText = text.ToString();
            if (string.IsNullOrWhiteSpace(fullText))
            {
                logger.LogWarning(AssistantLogMessages.EmptyAnswer, userId, toolset.ToolCalls);
                await AssistantUsageLimiter.RefundAsync(db, userId, today, CancellationToken.None);
                yield return AssistantEvent.Error(AssistantMessageKeys.NoAnswer, localizer[AssistantMessageKeys.NoAnswer].Value);
                yield break;
            }

            var spotIds = await VisibleCitedSpotsAsync(fullText, toolset.ReturnedSpotIds, cancellationToken);
            var answer = SpotMarker().Replace(fullText, string.Empty).Trim();

            logger.LogInformation(
                AssistantLogMessages.Answered, userId, stopwatch.ElapsedMilliseconds, toolset.ToolCalls, spotIds.Count, inputTokens, outputTokens);

            yield return AssistantEvent.Done(answer, spotIds);
        }

        // The system prompt stays fixed; the language, the location and the question travel together
        // in the last user message.
        private static List<ChatMessage> BuildMessages(Command command, int historyMessagesToModel)
        {
            var messages = new List<ChatMessage> { new(ChatRole.System, AssistantSystemPrompt.Text) };
            foreach (var message in (command.History ?? []).TakeLast(historyMessagesToModel))
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
