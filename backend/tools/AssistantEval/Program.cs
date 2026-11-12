using AssistantEval;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Assistant;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using FluentValidation;

EvalArguments arguments;
try
{
    arguments = EvalArguments.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

var cases = EvalCases.All
    .Where(c => arguments.Only.Count == 0 || arguments.Only.Any(o => c.Id.StartsWith(o, StringComparison.OrdinalIgnoreCase) || c.Group.Equals(o, StringComparison.OrdinalIgnoreCase)))
    .ToList();
if (cases.Count == 0)
{
    Console.Error.WriteLine("No question matches --only.");
    return 1;
}

if (arguments.ListOnly)
{
    Console.WriteLine($"{cases.Count} questions, {EvalSpots.All.Count} seeded spots.");
    foreach (var group in cases.GroupBy(c => c.Group))
    {
        Console.WriteLine();
        Console.WriteLine(group.Key);
        foreach (var c in group)
        {
            Console.WriteLine($"  [{c.Locale}] {c.Id}: {c.Question}");
        }
    }

    return 0;
}

if (string.IsNullOrWhiteSpace(arguments.ConnectionString))
{
    Console.Error.WriteLine("Give the database with --connection or the ASSISTANT_EVAL_CONNECTION_STRING environment variable.");
    Console.Error.WriteLine("It must be migrated; the run adds its \"[eval] \" spots and removes them again, so use a development or test database.");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var assistantOptions = configuration.GetSection("Assistant").Get<AssistantOptions>() ?? new AssistantOptions();
assistantOptions.Enabled = true;
assistantOptions.Provider = arguments.Provider ?? assistantOptions.Provider;
assistantOptions.Model = arguments.Model ?? assistantOptions.Model;
assistantOptions.OllamaEndpoint = arguments.OllamaEndpoint ?? assistantOptions.OllamaEndpoint;
// The limits are for real users; one run asks every question once.
assistantOptions.DailyQuestionsPerUser = int.MaxValue;
assistantOptions.DailyQuestionsTotal = int.MaxValue;

if (assistantOptions.Provider == AssistantProviders.Anthropic && !arguments.AllowPaid)
{
    Console.Error.WriteLine("The Anthropic provider costs money per question. Run again with --allow-paid when you want that.");
    return 1;
}

var userId = Guid.NewGuid();
await using var db = EvalDatabase.Connect(arguments.ConnectionString);

var services = new ServiceCollection();
services.AddLogging();
services.AddLocalization();
services.AddMediator(typeof(AssistantTools).Assembly);
services.AddValidatorsFromAssembly(typeof(AssistantTools).Assembly);
services.AddSingleton<IAppDbContext>(db);
services.AddSingleton<IUserContext>(new EvalUserContext(userId));
services.AddScoped<AssistantTools>();
services.Configure<SpotSearchOptions>(configuration.GetSection("SpotSearch"));
services.Configure<CommentOptions>(configuration.GetSection("Comments"));
await using var provider = services.BuildServiceProvider();

var handler = new AskAssistant.Handler(
    AssistantChatClient.Create(assistantOptions, anthropicApiKey: null),
    db,
    provider.GetRequiredService<IUserContext>(),
    provider.GetRequiredService<AssistantTools>(),
    Options.Create(assistantOptions),
    TimeProvider.System,
    provider.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SecretSpots.Features.Common.Localization.SharedResources>>(),
    NullLogger<AskAssistant.Handler>.Instance);

Console.WriteLine($"Model: {assistantOptions.Provider} / {assistantOptions.Model}. {cases.Count} questions.");
var outcomes = new List<EvalOutcome>();
try
{
    await EvalDatabase.CleanAsync(db, userId);
    var spotIds = await EvalDatabase.SeedAsync(db, userId);
    var runner = new EvalRunner(handler, spotIds);

    foreach (var evalCase in cases)
    {
        var outcome = await runner.RunAsync(evalCase);
        outcomes.Add(outcome);
        Console.WriteLine(EvalReport.Line(outcome));
        foreach (var problem in outcome.Problems)
        {
            Console.WriteLine($"    - {problem}");
        }
    }
}
finally
{
    await EvalDatabase.CleanAsync(db, userId);
}

Console.WriteLine();
Console.Write(EvalReport.Summary(outcomes));

var modelLabel = $"{assistantOptions.Provider} {assistantOptions.Model}";
var path = arguments.OutputPath
    ?? $"assistant-eval-{string.Concat(assistantOptions.Model.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'))}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md";
await File.WriteAllTextAsync(path, EvalReport.Markdown(outcomes, modelLabel, DateTimeOffset.UtcNow));
Console.WriteLine($"Report: {Path.GetFullPath(path)}");
return outcomes.All(o => o.Passed) ? 0 : 2;

internal sealed class EvalUserContext(Guid userId) : IUserContext
{
    public Guid UserId => userId;
    public Guid? UserIdOrNull => userId;
    public UserRole Role => UserRole.User;
}
