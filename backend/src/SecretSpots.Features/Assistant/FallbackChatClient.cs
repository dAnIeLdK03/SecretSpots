using System.ClientModel;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace SecretSpots.Features.Assistant;

// Every model in the chain has used up its quota for now: nothing is broken, the user just has to
// come back later.
public sealed class AssistantModelsExhaustedException(Exception? innerException = null)
    : Exception("Every assistant model has reached its rate limit.", innerException);

// Several models behind one IChatClient, tried in order. A model that refuses a request — its free
// quota is used up, or it is down — is set aside for a while and the same request goes to the next
// one, so the user only notices a slightly slower answer. Switching is only possible before a
// model has produced anything: a failure in the middle of an answer is passed on as it is.
public sealed class FallbackChatClient : IChatClient
{
    public sealed record Model(string Name, IChatClient Client);

    // How long a model that failed for a reason other than its quota is left alone.
    private static readonly TimeSpan ErrorCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxCooldown = TimeSpan.FromHours(24);

    private readonly IReadOnlyList<Model> _models;
    private readonly TimeSpan _quotaCooldown;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger? _logger;
    private readonly object _gate = new();
    private readonly DateTimeOffset[] _setAsideUntil;
    private readonly bool[] _setAsideForQuota;

    // quotaCooldown applies when a model reports a used-up quota without saying when to retry.
    public FallbackChatClient(IReadOnlyList<Model> models, TimeSpan quotaCooldown, TimeProvider timeProvider, ILogger? logger = null)
    {
        if (models.Count == 0)
        {
            throw new ArgumentException("At least one model is required.", nameof(models));
        }

        _models = models;
        _quotaCooldown = quotaCooldown;
        _timeProvider = timeProvider;
        _logger = logger;
        _setAsideUntil = new DateTimeOffset[models.Count];
        _setAsideForQuota = new bool[models.Count];
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Read once: the same messages may have to be sent to more than one model.
        var sent = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        Exception? lastFailure = null;

        for (var index = 0; index < _models.Count; index++)
        {
            if (IsSetAside(index))
            {
                continue;
            }

            await using var updates = _models[index].Client.GetStreamingResponseAsync(sent, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
            var started = false;
            while (true)
            {
                // `yield` can't sit inside a try/catch, so only the step that talks to the model is caught.
                bool hasNext;
                try
                {
                    hasNext = await updates.MoveNextAsync();
                }
                catch (Exception ex) when (!started && !cancellationToken.IsCancellationRequested)
                {
                    SetAside(index, ex);
                    lastFailure = ex;
                    break;
                }

                if (!hasNext)
                {
                    yield break;
                }

                started = true;
                yield return updates.Current;
            }
        }

        throw NothingAvailable(lastFailure);
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var sent = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        Exception? lastFailure = null;

        for (var index = 0; index < _models.Count; index++)
        {
            if (IsSetAside(index))
            {
                continue;
            }

            try
            {
                return await _models[index].Client.GetResponseAsync(sent, options, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                SetAside(index, ex);
                lastFailure = ex;
            }
        }

        throw NothingAvailable(lastFailure);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
        foreach (var model in _models)
        {
            model.Client.Dispose();
        }
    }

    private bool IsSetAside(int index)
    {
        lock (_gate)
        {
            return _setAsideUntil[index] > _timeProvider.GetUtcNow();
        }
    }

    private void SetAside(int index, Exception failure)
    {
        var quota = IsQuotaFailure(failure, out var retryAfter);
        var cooldown = quota ? retryAfter ?? _quotaCooldown : ErrorCooldown;
        if (cooldown > MaxCooldown)
        {
            cooldown = MaxCooldown;
        }

        lock (_gate)
        {
            _setAsideUntil[index] = _timeProvider.GetUtcNow() + cooldown;
            _setAsideForQuota[index] = quota;
        }

        _logger?.LogWarning(failure, AssistantLogMessages.ModelSetAside, _models[index].Name, quota, (int)cooldown.TotalSeconds);
    }

    // "Exhausted" only when every model is out because of its quota; if any of them is simply
    // failing, that is an outage and is reported as one.
    private Exception NothingAvailable(Exception? lastFailure)
    {
        lock (_gate)
        {
            return _setAsideForQuota.All(quota => quota)
                ? new AssistantModelsExhaustedException(lastFailure)
                : new InvalidOperationException("No assistant model is available.", lastFailure);
        }
    }

    // HTTP 429 from either kind of client, with the wait the provider asked for when it gave one.
    private static bool IsQuotaFailure(Exception failure, out TimeSpan? retryAfter)
    {
        retryAfter = null;
        for (var ex = failure; ex is not null; ex = ex.InnerException)
        {
            switch (ex)
            {
                case ClientResultException { Status: (int)HttpStatusCode.TooManyRequests } refused:
                    if (refused.GetRawResponse()?.Headers.TryGetValue("retry-after", out var value) == true
                        && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                        && seconds > 0)
                    {
                        retryAfter = TimeSpan.FromSeconds(seconds);
                    }

                    return true;
                case HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests }:
                    return true;
            }
        }

        return false;
    }
}
