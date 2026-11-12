using SecretSpots.Features.Common.Configuration;

namespace AssistantEval;

// Command line: `dotnet run --project tools/AssistantEval -- [list] [--connection <string>] [--provider Ollama|Anthropic]
// [--model <name>[,<name>...]] [--ollama-endpoint <url>] [--only <id or group>[,...]] [--out-dir <folder>] [--allow-paid]`,
// or `... -- compare <run.json> <run.json> [...]` to line up the results of earlier runs.
public class EvalArguments
{
    public bool ListOnly { get; private init; }
    public IReadOnlyList<string> CompareFiles { get; private init; } = [];
    public bool Compare { get; private init; }
    public string? ConnectionString { get; private init; }
    public string? Provider { get; private init; }
    // One or more models, run one after the other against the same data.
    public IReadOnlyList<string> Models { get; private init; } = [];
    public string? OllamaEndpoint { get; private init; }
    public IReadOnlyList<string> Only { get; private init; } = [];
    public string OutputDirectory { get; private init; } = ".";
    public bool AllowPaid { get; private init; }

    public static EvalArguments Parse(string[] args)
    {
        string? connection = Environment.GetEnvironmentVariable("ASSISTANT_EVAL_CONNECTION_STRING");
        string? provider = null, endpoint = null;
        var output = ".";
        List<string> models = [], only = [], compareFiles = [];
        bool list = false, compare = false, allowPaid = false;

        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");

            switch (args[i])
            {
                case "list": list = true; break;
                case "compare": compare = true; break;
                case "--connection": connection = Value(); break;
                case "--provider": provider = Value(); break;
                case "--model": models.AddRange(Value().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); break;
                case "--ollama-endpoint": endpoint = Value(); break;
                case "--only": only.AddRange(Value().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); break;
                case "--out-dir": output = Value(); break;
                case "--allow-paid": allowPaid = true; break;
                case var file when compare && !file.StartsWith("--"): compareFiles.Add(file); break;
                default: throw new ArgumentException($"Unknown argument '{args[i]}'.");
            }
        }

        if (provider is not null && provider is not (AssistantProviders.Ollama or AssistantProviders.Anthropic))
        {
            throw new ArgumentException($"--provider must be {AssistantProviders.Ollama} or {AssistantProviders.Anthropic}.");
        }

        return new EvalArguments
        {
            ListOnly = list, Compare = compare, CompareFiles = compareFiles, ConnectionString = connection, Provider = provider, Models = models,
            OllamaEndpoint = endpoint, Only = only, OutputDirectory = output, AllowPaid = allowPaid,
        };
    }
}
