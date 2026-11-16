namespace SecretSpots.Features.Common.Configuration;

public static class AssistantProviders
{
    // A model running locally in Ollama: free, for development.
    public const string Ollama = "Ollama";

    // Claude through the Anthropic API; the key comes from Anthropic:ApiKey or ANTHROPIC_API_KEY.
    public const string Anthropic = "Anthropic";

    // Any hosted service that speaks the OpenAI chat API (Groq, Cerebras, ...): needs Endpoint and ApiKey.
    public const string OpenAICompatible = "OpenAICompatible";
}

// One model the assistant can talk to.
public class AssistantModelOptions
{
    public string Provider { get; set; } = AssistantProviders.Ollama;
    public string Model { get; set; } = string.Empty;
    // The service's base address, e.g. https://api.groq.com/openai/v1. Required for OpenAICompatible;
    // for Ollama it overrides OllamaEndpoint.
    public string? Endpoint { get; set; }
    // From user-secrets or the environment — never appsettings.
    public string? ApiKey { get; set; }
}

public class AssistantOptions
{
    // Off until deliberately switched on: the endpoint answers 404 while this is false.
    public bool Enabled { get; set; }

    public string Provider { get; set; } = AssistantProviders.Ollama;
    public string Model { get; set; } = "qwen2.5:7b-instruct";
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    // Context window asked of Ollama, in tokens. Its default is small, and a prompt that doesn't fit
    // is cut from the start — the system prompt first — so the model forgets its rules and loops.
    public int OllamaContextTokens { get; set; } = 8192;

    // Endpoint and key of the model above when it is a hosted one (see AssistantModelOptions).
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }

    // Models tried, in this order, when the one above refuses a question — its free quota is used
    // up, or it is down. Empty means there is only the one model.
    public List<AssistantModelOptions> Fallbacks { get; set; } = [];
    // How long a model with a used-up quota is skipped when it doesn't say when to try again.
    public int QuotaCooldownMinutes { get; set; } = 10;

    public int DailyQuestionsPerUser { get; set; } = 30;
    // Across all users — the cost ceiling once a paid model is in use.
    public int DailyQuestionsTotal { get; set; } = 1000;
    public int MaxToolCallsPerQuestion { get; set; } = 5;

    public int MaxQuestionLength { get; set; } = 500;
    // Earlier turns the client may send back for context; assistant answers can run longer.
    public int MaxHistoryMessages { get; set; } = 10;
    public int MaxHistoryMessageLength { get; set; } = 2000;
    // How many of those earlier turns actually reach the model (the most recent ones): a small model
    // does better with a short history than with a long one it starts to echo.
    public int HistoryMessagesToModel { get; set; } = 6;

    public int MaxOutputTokens { get; set; } = 2048;
    // A local model on a CPU can need a minute per answer; a hosted one is far faster.
    public int TimeoutSeconds { get; set; } = 120;
}
