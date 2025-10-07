using OpenAI;
using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InsightStream.Application.Interfaces.Factories;
using InsightStream.Infrastructure.Configuration;

namespace InsightStream.Infrastructure.Factories;

public sealed class ChatClientFactory : IChatClientFactory
{
    private readonly ProvidersConfiguration _providersConfig;
    private readonly string _defaultProvider;
    private readonly ILogger<ChatClientFactory> _logger;

    public ChatClientFactory(
        IOptions<ProvidersConfiguration> providersConfig,
        IOptions<AppConfiguration> appConfig,
        ILogger<ChatClientFactory> logger)
    {
        _providersConfig = providersConfig.Value;
        _defaultProvider = appConfig.Value.DefaultProvider;
        _logger = logger;
    }

    public IChatClient CreateClient(string? providerName = null, string? modelId = null)
    {
        // Use provided provider or fall back to default
        var provider = providerName ?? _defaultProvider;
        
        if (!_providersConfig.Providers.TryGetValue(provider, out var settings))
        {
            throw new InvalidOperationException(
                $"Provider '{provider}' not found in configuration. Available providers: {string.Join(", ", _providersConfig.Providers.Keys)}");
        }

        // Use provided model or default to first configured model
        var model = modelId ?? settings.Models.FirstOrDefault()?.Id;
        
        if (string.IsNullOrEmpty(model))
        {
            throw new InvalidOperationException(
                $"No models configured for provider '{provider}'");
        }

        _logger.LogInformation(
            "Creating chat client for provider '{Provider}' with model '{Model}'",
            provider,
            model);

        return CreateOpenRouterClient(settings, model);
    }

    private IChatClient CreateOpenRouterClient(ProviderSettings settings, string modelId)
    {
        // Create the OpenAI client with custom endpoint support
        var credential = new ApiKeyCredential(settings.ApiKey);
        var options = new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) };
        var openAIClient = new OpenAIClient(credential, options);

        // Get the chat client
        var chatClient = openAIClient.GetChatClient(modelId);

        // Convert to IChatClient and add OpenTelemetry support
        return chatClient.AsIChatClient().AsBuilder()
            .UseOpenTelemetry()
            .Build();
    }
}