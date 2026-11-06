namespace SecretSpots.Features.Common.Configuration;

public static class AssistantProviders
{
    // A model running locally in Ollama: free, for development.
    public const string Ollama = "Ollama";

    // Claude through the Anthropic API; the key comes from Anthropic:ApiKey or ANTHROPIC_API_KEY.
    public const string Anthropic = "Anthropic";
}

public class AssistantOptions
{
    // Off until deliberately switched on: the endpoint answers 404 while this is false.
    public bool Enabled { get; set; }

    public string Provider { get; set; } = AssistantProviders.Ollama;
    public string Model { get; set; } = "qwen2.5:7b-instruct";
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    public int DailyQuestionsPerUser { get; set; } = 30;
    // Across all users — the cost ceiling once a paid model is in use.
    public int DailyQuestionsTotal { get; set; } = 1000;
    public int MaxToolCallsPerQuestion { get; set; } = 5;

    public int MaxQuestionLength { get; set; } = 500;
    // Earlier turns the client may send back for context; assistant answers can run longer.
    public int MaxHistoryMessages { get; set; } = 10;
    public int MaxHistoryMessageLength { get; set; } = 2000;

    public int MaxOutputTokens { get; set; } = 2048;
    // A local model on a CPU can need a minute per answer; a hosted one is far faster.
    public int TimeoutSeconds { get; set; } = 120;
}
