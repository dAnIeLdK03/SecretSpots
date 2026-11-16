using System.ClientModel;
using System.ClientModel.Primitives;
using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using SecretSpots.Features.Common.Configuration;

namespace SecretSpots.Features.Assistant;

// Builds the IChatClient the assistant talks to, from configuration alone: the model named by
// Assistant:Provider/Model, then any Assistant:Fallbacks, tried in that order when the one before
// refuses a question (see FallbackChatClient). A model can be a local Ollama one, Claude, or any
// hosted service with an OpenAI-compatible API. The tool-calling loop (FunctionInvokingChatClient)
// sits on top, the same for all of them.
public static class AssistantChatClient
{
    // Registered as a keyed service, so the assistant's client can't be mistaken for any other.
    public const string ServiceKey = "assistant";

    public static IChatClient Create(
        AssistantOptions options, string? anthropicApiKey, ILoggerFactory? loggerFactory = null, TimeProvider? timeProvider = null)
    {
        var first = new AssistantModelOptions { Provider = options.Provider, Model = options.Model, Endpoint = options.Endpoint, ApiKey = options.ApiKey };
        var models = options.Fallbacks.Prepend(first)
            .Select(model => new FallbackChatClient.Model($"{model.Provider}/{model.Model}", CreateModel(model, options, anthropicApiKey)))
            .ToList();

        var chain = new FallbackChatClient(
            models,
            TimeSpan.FromMinutes(options.QuotaCooldownMinutes),
            timeProvider ?? TimeProvider.System,
            loggerFactory?.CreateLogger<FallbackChatClient>());
        return WithToolLoop(chain, options, loggerFactory);
    }

    // AssistantToolset enforces the real tool-call limit (and lets the model still answer); this cap
    // only stops a model that keeps calling tools after being told to answer.
    public static IChatClient WithToolLoop(IChatClient model, AssistantOptions options, ILoggerFactory? loggerFactory = null) =>
        model.AsBuilder()
            .UseFunctionInvocation(loggerFactory, invoker => invoker.MaximumIterationsPerRequest = options.MaxToolCallsPerQuestion + 2)
            .Build();

    private static IChatClient CreateModel(AssistantModelOptions model, AssistantOptions options, string? anthropicApiKey)
    {
        if (string.IsNullOrWhiteSpace(model.Model))
        {
            throw new InvalidOperationException($"An assistant model with provider '{model.Provider}' has no Model set.");
        }

        var apiKey = string.IsNullOrWhiteSpace(model.ApiKey) ? null : model.ApiKey;
        var endpoint = string.IsNullOrWhiteSpace(model.Endpoint) ? null : model.Endpoint;
        switch (model.Provider)
        {
            case AssistantProviders.Ollama:
                // Ollama's own request settings, here the size of its context window, ride along
                // on every request to it — and only to it.
                return ((IChatClient)new OllamaApiClient(new Uri(endpoint ?? options.OllamaEndpoint), model.Model))
                    .AsBuilder()
                    .ConfigureOptions(chatOptions => chatOptions.RawRepresentationFactory =
                        _ => new ChatRequest { Options = new OllamaSharp.Models.RequestOptions { NumCtx = options.OllamaContextTokens } })
                    .Build();

            case AssistantProviders.Anthropic:
                // Without a configured key the SDK falls back to ANTHROPIC_API_KEY (or the other sources it supports).
                var anthropicKey = apiKey ?? (string.IsNullOrWhiteSpace(anthropicApiKey) ? null : anthropicApiKey);
                return (anthropicKey is null ? new AnthropicClient() : new AnthropicClient { ApiKey = anthropicKey })
                    .AsIChatClient(model.Model, options.MaxOutputTokens);

            case AssistantProviders.OpenAICompatible:
                if (endpoint is null || apiKey is null)
                {
                    throw new InvalidOperationException($"Assistant model '{model.Model}' ({AssistantProviders.OpenAICompatible}) needs both Endpoint and ApiKey.");
                }

                // No retries inside the client: a refused request should move on to the next
                // model at once instead of waiting here.
                return new OpenAI.Chat.ChatClient(
                        model.Model,
                        new ApiKeyCredential(apiKey),
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint), RetryPolicy = new ClientRetryPolicy(0) })
                    .AsIChatClient();

            default:
                throw new InvalidOperationException(
                    $"Unknown assistant provider '{model.Provider}'. Use '{AssistantProviders.Ollama}', '{AssistantProviders.Anthropic}' or '{AssistantProviders.OpenAICompatible}'.");
        }
    }
}
