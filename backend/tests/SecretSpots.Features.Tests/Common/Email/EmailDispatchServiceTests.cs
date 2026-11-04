using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SecretSpots.Features.Common.Email;

namespace SecretSpots.Features.Tests.Common.Email;

public class EmailDispatchServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static (EmailQueue Queue, EmailDispatchService Service) Create(IEmailTransport transport, TimeProvider time)
    {
        var services = new ServiceCollection().AddSingleton(transport).BuildServiceProvider();
        var queue = new EmailQueue(NullLogger<EmailQueue>.Instance);
        var service = new EmailDispatchService(
            queue, services.GetRequiredService<IServiceScopeFactory>(), time, NullLogger<EmailDispatchService>.Instance);
        return (queue, service);
    }

    [Fact]
    public async Task Queued_emails_are_delivered_in_order()
    {
        var transport = new RecordingTransport();
        var (queue, service) = Create(transport, TimeProvider.System);
        await service.StartAsync(CancellationToken.None);

        await queue.SendAsync("a@example.com", "First", "<p>1</p>", CancellationToken.None);
        await queue.SendAsync("b@example.com", "Second", "<p>2</p>", CancellationToken.None);
        await transport.WaitForAsync(2, Timeout);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(["First", "Second"], transport.Delivered.Select(m => m.Subject));
    }

    [Fact]
    public async Task A_failed_send_is_retried_after_a_delay()
    {
        var transport = new RecordingTransport { FailuresBeforeSuccess = 1 };
        var time = new FakeTimeProvider();
        var (queue, service) = Create(transport, time);
        await service.StartAsync(CancellationToken.None);

        await queue.SendAsync("a@example.com", "Hello", "<p>hi</p>", CancellationToken.None);
        await transport.WaitForAttemptsAsync(1, Timeout);
        Assert.Empty(transport.Delivered);
        // Keep nudging the fake clock until the retry delay elapses and the second attempt lands.
        await AdvanceUntilAsync(time, () => transport.Delivered.Count == 1);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal("Hello", Assert.Single(transport.Delivered).Subject);
    }

    [Fact]
    public async Task An_email_that_keeps_failing_is_dropped_and_does_not_block_the_next()
    {
        var transport = new RecordingTransport { AlwaysFailFor = "broken@example.com" };
        var time = new FakeTimeProvider();
        var (queue, service) = Create(transport, time);
        await service.StartAsync(CancellationToken.None);

        await queue.SendAsync("broken@example.com", "Lost", "<p>x</p>", CancellationToken.None);
        await queue.SendAsync("ok@example.com", "Delivered", "<p>y</p>", CancellationToken.None);
        await AdvanceUntilAsync(time, () => transport.Delivered.Count == 1);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal("Delivered", Assert.Single(transport.Delivered).Subject);
        Assert.Equal(3, transport.AttemptsFor("broken@example.com"));
    }

    [Fact]
    public async Task Stopping_delivers_what_is_still_queued()
    {
        var transport = new RecordingTransport();
        var (queue, service) = Create(transport, TimeProvider.System);
        // Queued before the dispatcher even starts, so it's all still pending at shutdown.
        for (var i = 0; i < 5; i++)
        {
            await queue.SendAsync($"user{i}@example.com", $"Mail {i}", "<p></p>", CancellationToken.None);
        }

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(5, transport.Delivered.Count);
    }

    [Fact]
    public async Task Queueing_after_shutdown_is_ignored_instead_of_failing_the_request()
    {
        var (queue, _) = Create(new RecordingTransport(), TimeProvider.System);
        queue.Complete();

        await queue.SendAsync("late@example.com", "Too late", "<p></p>", CancellationToken.None);
    }

    private static async Task AdvanceUntilAsync(FakeTimeProvider time, Func<bool> done)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!done())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for the dispatcher.");
            time.Advance(TimeSpan.FromSeconds(1));
            await Task.Delay(10);
        }
    }

    private sealed class RecordingTransport : IEmailTransport
    {
        private readonly object _lock = new();
        private readonly List<EmailMessage> _delivered = [];
        private readonly Dictionary<string, int> _attempts = [];

        public int FailuresBeforeSuccess { get; init; }

        public string? AlwaysFailFor { get; init; }

        public IReadOnlyList<EmailMessage> Delivered
        {
            get { lock (_lock) { return _delivered.ToList(); } }
        }

        public int AttemptsFor(string toEmail)
        {
            lock (_lock) { return _attempts.GetValueOrDefault(toEmail); }
        }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var attempt = _attempts[message.ToEmail] = _attempts.GetValueOrDefault(message.ToEmail) + 1;
                if (message.ToEmail == AlwaysFailFor || attempt <= FailuresBeforeSuccess)
                {
                    throw new HttpRequestException("Email provider unavailable");
                }

                _delivered.Add(message);
                return Task.CompletedTask;
            }
        }

        public async Task WaitForAsync(int count, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (Delivered.Count < count)
            {
                Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for deliveries.");
                await Task.Delay(10);
            }
        }

        public async Task WaitForAttemptsAsync(int count, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (true)
            {
                lock (_lock)
                {
                    if (_attempts.Values.Sum() >= count) return;
                }

                Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for send attempts.");
                await Task.Delay(10);
            }
        }
    }
}
