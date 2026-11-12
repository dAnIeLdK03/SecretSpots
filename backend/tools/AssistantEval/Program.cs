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

if (arguments.Compare)
{
    if (arguments.CompareFiles.Count < 2)
    {
        Console.Error.WriteLine("compare needs at least two run files (the .json files a run writes).");
        return 1;
    }

    var earlierRuns = new List<EvalRun>();
    foreach (var file in arguments.CompareFiles)
    {
        earlierRuns.Add(await EvalRun.LoadAsync(file));
    }

    Console.Write(EvalComparison.Summary(earlierRuns));
    Directory.CreateDirectory(arguments.OutputDirectory);
    var earlierPath = Path.Combine(arguments.OutputDirectory, $"assistant-eval-compare-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md");
    await File.WriteAllTextAsync(earlierPath, EvalComparison.Markdown(earlierRuns));
    Console.WriteLine($"Comparison: {Path.GetFullPath(earlierPath)}");
    return 0;
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

var baseOptions = configuration.GetSection("Assistant").Get<AssistantOptions>() ?? new AssistantOptions();
baseOptions.Enabled = true;
baseOptions.Provider = arguments.Provider ?? baseOptions.Provider;
baseOptions.OllamaEndpoint = arguments.OllamaEndpoint ?? baseOptions.OllamaEndpoint;
// The limits are for real users; one run asks every question once.
baseOptions.DailyQuestionsPerUser = int.MaxValue;
baseOptions.DailyQuestionsTotal = int.MaxValue;

var models = arguments.Models.Count > 0 ? arguments.Models : [baseOptions.Model];

if (baseOptions.Provider == AssistantProviders.Anthropic && !arguments.AllowPaid)
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

Directory.CreateDirectory(arguments.OutputDirectory);
var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
var finished = new List<EvalRun>();
try
{
    await EvalDatabase.CleanAsync(db, userId);
    var spotIds = await EvalDatabase.SeedAsync(db, userId);

    foreach (var model in models)
    {
        // The same settings for every model; only the model differs.
        var options = new AssistantOptions
        {
            Enabled = true,
            Provider = baseOptions.Provider,
            Model = model,
            OllamaEndpoint = baseOptions.OllamaEndpoint,
            OllamaContextTokens = baseOptions.OllamaContextTokens,
            DailyQuestionsPerUser = baseOptions.DailyQuestionsPerUser,
            DailyQuestionsTotal = baseOptions.DailyQuestionsTotal,
            MaxToolCallsPerQuestion = baseOptions.MaxToolCallsPerQuestion,
            MaxQuestionLength = baseOptions.MaxQuestionLength,
            MaxHistoryMessages = baseOptions.MaxHistoryMessages,
            MaxHistoryMessageLength = baseOptions.MaxHistoryMessageLength,
            HistoryMessagesToModel = baseOptions.HistoryMessagesToModel,
            MaxOutputTokens = baseOptions.MaxOutputTokens,
            TimeoutSeconds = baseOptions.TimeoutSeconds,
        };

        var handler = new AskAssistant.Handler(
            AssistantChatClient.Create(options, anthropicApiKey: null),
            db,
            provider.GetRequiredService<IUserContext>(),
            provider.GetRequiredService<AssistantTools>(),
            Options.Create(options),
            TimeProvider.System,
            provider.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SecretSpots.Features.Common.Localization.SharedResources>>(),
            NullLogger<AskAssistant.Handler>.Instance);
        var runner = new EvalRunner(handler, spotIds);

        var label = $"{options.Provider} {model}";
        Console.WriteLine();
        Console.WriteLine($"Model: {label}. {cases.Count} questions.");
        var outcomes = new List<EvalOutcome>();
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

        Console.WriteLine();
        Console.Write(EvalReport.Summary(outcomes));

        var run = EvalRun.From(outcomes, label, DateTimeOffset.UtcNow);
        finished.Add(run);
        var fileName = Path.Combine(arguments.OutputDirectory,
            $"assistant-eval-{string.Concat(model.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'))}-{stamp}");
        await File.WriteAllTextAsync(fileName + ".md", EvalReport.Markdown(outcomes, label, run.At));
        await run.SaveAsync(fileName + ".json");
        Console.WriteLine($"Report: {Path.GetFullPath(fileName + ".md")}");
    }
}
finally
{
    await EvalDatabase.CleanAsync(db, userId);
}

if (finished.Count > 1)
{
    Console.WriteLine();
    Console.Write(EvalComparison.Summary(finished));
    var comparePath = Path.Combine(arguments.OutputDirectory, $"assistant-eval-compare-{stamp}.md");
    await File.WriteAllTextAsync(comparePath, EvalComparison.Markdown(finished));
    Console.WriteLine($"Comparison: {Path.GetFullPath(comparePath)}");
}

return finished.All(r => r.Passed == r.Results.Count) ? 0 : 2;

internal sealed class EvalUserContext(Guid userId) : IUserContext
{
    public Guid UserId => userId;
    public Guid? UserIdOrNull => userId;
    public UserRole Role => UserRole.User;
}
