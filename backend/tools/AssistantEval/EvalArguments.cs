using SecretSpots.Features.Common.Configuration;

namespace AssistantEval;

// Command line: `dotnet run --project tools/AssistantEval -- [list] [--connection <string>] [--provider Ollama|Anthropic]
// [--model <name>] [--ollama-endpoint <url>] [--only <id or group>[,...]] [--out <file>] [--allow-paid]`.
public class EvalArguments
{
    public bool ListOnly { get; private init; }
    public string? ConnectionString { get; private init; }
    public string? Provider { get; private init; }
    public string? Model { get; private init; }
    public string? OllamaEndpoint { get; private init; }
    public IReadOnlyList<string> Only { get; private init; } = [];
    public string? OutputPath { get; private init; }
    public bool AllowPaid { get; private init; }

    public static EvalArguments Parse(string[] args)
    {
        string? connection = Environment.GetEnvironmentVariable("ASSISTANT_EVAL_CONNECTION_STRING");
        string? provider = null, model = null, endpoint = null, output = null;
        List<string> only = [];
        bool list = false, allowPaid = false;

        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");

            switch (args[i])
            {
                case "list": list = true; break;
                case "--connection": connection = Value(); break;
                case "--provider": provider = Value(); break;
                case "--model": model = Value(); break;
                case "--ollama-endpoint": endpoint = Value(); break;
                case "--only": only.AddRange(Value().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); break;
                case "--out": output = Value(); break;
                case "--allow-paid": allowPaid = true; break;
                default: throw new ArgumentException($"Unknown argument '{args[i]}'.");
            }
        }

        if (provider is not null && provider is not (AssistantProviders.Ollama or AssistantProviders.Anthropic))
        {
            throw new ArgumentException($"--provider must be {AssistantProviders.Ollama} or {AssistantProviders.Anthropic}.");
        }

        return new EvalArguments
        {
            ListOnly = list, ConnectionString = connection, Provider = provider, Model = model,
            OllamaEndpoint = endpoint, Only = only, OutputPath = output, AllowPaid = allowPaid,
        };
    }
}
