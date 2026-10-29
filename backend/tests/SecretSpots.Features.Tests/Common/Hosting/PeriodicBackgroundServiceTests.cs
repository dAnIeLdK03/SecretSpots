using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Features.Common.Hosting;

namespace SecretSpots.Features.Tests.Common.Hosting;

public class PeriodicBackgroundServiceTests
{
    [Fact]
    public async Task A_failing_run_is_swallowed_and_the_next_interval_runs_again()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var service = new FlakyService(services.GetRequiredService<IServiceScopeFactory>());

        await service.StartAsync(CancellationToken.None);
        var reachedSecondRun = await Task.WhenAny(service.SecondRun.Task, Task.Delay(TimeSpan.FromSeconds(5))) == service.SecondRun.Task;
        await service.StopAsync(CancellationToken.None);

        Assert.True(reachedSecondRun);
        // ExecuteTask faulting is exactly what StopHost would turn into an API shutdown.
        Assert.False(service.ExecuteTask!.IsFaulted);
    }

    [Fact]
    public async Task Each_run_gets_its_own_scope()
    {
        using var services = new ServiceCollection().AddScoped<ScopeMarker>().BuildServiceProvider();
        var service = new FlakyService(services.GetRequiredService<IServiceScopeFactory>());

        await service.StartAsync(CancellationToken.None);
        await Task.WhenAny(service.SecondRun.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        await service.StopAsync(CancellationToken.None);

        Assert.True(service.Markers.Count >= 2);
        Assert.NotSame(service.Markers[0], service.Markers[1]);
    }

    private sealed class ScopeMarker;

    // Throws on its first run, completes SecondRun on the next one.
    private sealed class FlakyService(IServiceScopeFactory scopeFactory)
        : PeriodicBackgroundService(scopeFactory, NullLogger.Instance)
    {
        private int _runs;

        public TaskCompletionSource SecondRun { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<object?> Markers { get; } = [];

        protected override TimeSpan Interval => TimeSpan.FromMilliseconds(20);

        protected override Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            Markers.Add(services.GetService<ScopeMarker>());
            if (Interlocked.Increment(ref _runs) == 1)
            {
                throw new InvalidOperationException("Simulated database blip");
            }

            SecondRun.TrySetResult();
            return Task.CompletedTask;
        }
    }
}
