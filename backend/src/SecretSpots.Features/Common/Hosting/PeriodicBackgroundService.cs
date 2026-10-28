using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SecretSpots.Features.Common.Hosting;

// A job that runs once at startup and then every Interval for the lifetime of the process — the
// app has no external scheduler, so periodic work (token cleanup, soft-deleting expired rejected
// spots, ...) lives in-process. Each run gets its own DI scope (a fresh scoped DbContext).
//
// A failed run is logged and the loop carries on. Letting the exception escape ExecuteAsync would
// hit the host's default BackgroundServiceExceptionBehavior.StopHost and take the whole API down
// over e.g. a momentary database blip during a cleanup.
public abstract class PeriodicBackgroundService(IServiceScopeFactory scopeFactory, ILogger logger) : BackgroundService
{
    protected ILogger Logger { get; } = logger;

    protected abstract TimeSpan Interval { get; }

    protected abstract Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                await RunSafelyAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task RunSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await RunOnceAsync(scope.ServiceProvider, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            Logger.LogError(ex, HostingLogMessages.PeriodicRunFailed, GetType().Name);
        }
    }
}
