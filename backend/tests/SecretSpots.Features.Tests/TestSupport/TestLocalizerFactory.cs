using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Features.Common.Localization;

namespace SecretSpots.Features.Tests.TestSupport;

internal static class TestLocalizerFactory
{
    public static IStringLocalizer<SharedResources> Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResources>>();
    }
}
