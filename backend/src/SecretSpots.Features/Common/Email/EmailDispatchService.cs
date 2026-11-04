using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SecretSpots.Features.Common.Email;

// Delivers queued emails one at a time. A failed send is retried a couple of times (the provider
// is usually only briefly unavailable), then logged and dropped so it never blocks the next one.
// On shutdown it keeps going until the queue is empty or the host's shutdown timeout runs out.
public class EmailDispatchService(
    EmailQueue queue,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<EmailDispatchService> logger) : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)];
    private static readonly int MaxAttempts = RetryDelays.Length + 1;

    // Cancelled only when the host gives up waiting at shutdown, not when stopping starts —
    // so the queue can still drain during a normal shutdown.
    private readonly CancellationTokenSource _abort = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Ends once Complete() was called and everything queued before it was handled.
            await foreach (var message in queue.Reader.ReadAllAsync(_abort.Token))
            {
                await SendWithRetriesAsync(message, _abort.Token);
            }
        }
        catch (OperationCanceledException) when (_abort.IsCancellationRequested)
        {
            // Shutdown timeout reached with emails still queued; nothing more we can do.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        queue.Complete();
        // base.StopAsync waits for ExecuteAsync to finish draining until `cancellationToken` (the
        // host's shutdown timeout) fires; only then are in-flight sends aborted.
        await using var registration = cancellationToken.Register(_abort.Cancel);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _abort.Dispose();
        base.Dispose();
    }

    private async Task SendWithRetriesAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var transport = scope.ServiceProvider.GetRequiredService<IEmailTransport>();
                await transport.SendAsync(message, cancellationToken);
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == MaxAttempts)
                {
                    logger.LogError(ex, EmailLogMessages.SendFailed, message.ToEmail, MaxAttempts);
                    return;
                }

                var delay = RetryDelays[attempt - 1];
                logger.LogWarning(ex, EmailLogMessages.SendAttemptFailed, message.ToEmail, attempt, MaxAttempts, delay);
                await Task.Delay(delay, timeProvider, cancellationToken);
            }
        }
    }
}
