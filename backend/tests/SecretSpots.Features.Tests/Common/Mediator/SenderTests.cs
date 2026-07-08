using Microsoft.Extensions.DependencyInjection;
using SecretSpots.Features.Common.Mediator;

namespace SecretSpots.Features.Tests.Common.Mediator;

public class SenderTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken) =>
            Task.FromResult($"pong: {request.Message}");
    }

    [Fact]
    public async Task Send_dispatches_to_the_registered_handler()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(SenderTests).Assembly);

        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new Ping("hello"));

        Assert.Equal("pong: hello", result);
    }
}
