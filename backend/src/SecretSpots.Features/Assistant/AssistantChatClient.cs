using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using SecretSpots.Features.Common.Configuration;

namespace SecretSpots.Features.Assistant;

// Builds the IChatClient the assistant talks to: a local Ollama model for development, or Claude
// once the app goes live — switched by Assistant:Provider/Assistant:Model alone. The tool-calling
// loop (FunctionInvokingChatClient) sits on top, the same for either provider.
public static class AssistantChatClient
{
    // Registered as a keyed service, so the assistant's client can't be mistaken for any other.
    public const string ServiceKey = "assistant";

    public static IChatClient Create(AssistantOptions options, string? anthropicApiKey, ILoggerFactory? loggerFactory = null)
    {
        IChatClient model = options.Provider switch
        {
            AssistantProviders.Ollama => new OllamaApiClient(new Uri(options.OllamaEndpoint), options.Model),
            // Without a configured key the SDK falls back to ANTHROPIC_API_KEY (or the other sources it supports).
            AssistantProviders.Anthropic => (string.IsNullOrWhiteSpace(anthropicApiKey) ? new AnthropicClient() : new AnthropicClient { ApiKey = anthropicApiKey })
                .AsIChatClient(options.Model, options.MaxOutputTokens),
            _ => throw new InvalidOperationException($"Unknown Assistant:Provider '{options.Provider}'. Use '{AssistantProviders.Ollama}' or '{AssistantProviders.Anthropic}'."),
        };

        return WithToolLoop(model, options, loggerFactory);
    }

    // Provider-specific request settings. Only Ollama needs any: the size of its context window.
    public static void ApplyProviderOptions(ChatOptions chatOptions, AssistantOptions options)
    {
        if (options.Provider == AssistantProviders.Ollama)
        {
            chatOptions.RawRepresentationFactory = _ => new ChatRequest { Options = new RequestOptions { NumCtx = options.OllamaContextTokens } };
        }
    }

    // AssistantToolset enforces the real tool-call limit (and lets the model still answer); this cap
    // only stops a model that keeps calling tools after being told to answer.
    public static IChatClient WithToolLoop(IChatClient model, AssistantOptions options, ILoggerFactory? loggerFactory = null) =>
        model.AsBuilder()
            .UseFunctionInvocation(loggerFactory, invoker => invoker.MaximumIterationsPerRequest = options.MaxToolCallsPerQuestion + 2)
            .Build();
}
