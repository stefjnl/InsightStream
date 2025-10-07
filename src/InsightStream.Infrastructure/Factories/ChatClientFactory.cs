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
        // Validate configuration dependencies
        if (_providersConfig == null)
        {
            throw new InvalidOperationException("Providers configuration is not initialized. Ensure the configuration is properly loaded.");
        }

        if (_providersConfig.Providers == null || _providersConfig.Providers.Count == 0)
        {
            throw new InvalidOperationException("No providers are configured. Please add provider configurations in your settings.");
        }

        if (string.IsNullOrWhiteSpace(_defaultProvider))
        {
            throw new InvalidOperationException("Default provider is not configured. Please set a default provider in your configuration.");
        }

        // Use provided provider or fall back to default
        var provider = providerName ?? _defaultProvider;
        
        if (!_providersConfig.Providers.TryGetValue(provider, out var settings))
        {
            throw new InvalidOperationException(
                $"Provider '{provider}' not found in configuration. Available providers: {string.Join(", ", _providersConfig.Providers.Keys)}");
        }

        // Validate provider settings
        if (settings == null)
        {
            throw new InvalidOperationException($"Provider settings for '{provider}' is null. Please check your configuration.");
        }

        if (settings.Models == null || settings.Models.Count == 0)
        {
            throw new InvalidOperationException(
                $"No models configured for provider '{provider}'. Please add at least one model configuration.");
        }

        // Use provided model or default to first configured model
        var model = modelId ?? settings.Models.FirstOrDefault()?.Id;
        
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                $"No valid model configured for provider '{provider}'. The model ID is null or empty.");
        }

        // Verify the requested model exists in the provider's configuration
        if (!string.IsNullOrWhiteSpace(modelId) && !settings.Models.Any(m => string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Model '{modelId}' is not configured for provider '{provider}'. Available models: {string.Join(", ", settings.Models.Select(m => m.Id))}");
        }

        _logger.LogInformation(
            "Creating chat client for provider '{Provider}' with model '{Model}'",
            provider,
            model);

        try
        {
            var client = CreateOpenRouterClient(settings, model);
            
            // Final validation to ensure we never return a null client
            if (client == null)
            {
                throw new InvalidOperationException(
                    $"Chat client creation returned null for provider '{provider}' and model '{model}'. This indicates a critical failure in the client creation process.");
            }

            return client;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException || ex is ArgumentNullException))
        {
            _logger.LogError(ex, "Unexpected error occurred while creating chat client for provider '{Provider}' and model '{Model}'",
                provider, model);
            throw new InvalidOperationException(
                $"Failed to create chat client for provider '{provider}' and model '{model}': {ex.Message}", ex);
        }
    }

    private IChatClient CreateOpenRouterClient(ProviderSettings settings, string modelId)
    {
        // Validate input parameters
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings), "Provider settings cannot be null");
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        }

        // Validate critical configuration properties
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException(
                $"API key is missing or empty for provider. Please configure a valid API key.");
        }

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException(
                $"Endpoint is missing or empty for provider. Please configure a valid endpoint URL.");
        }

        // Validate endpoint format
        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new InvalidOperationException(
                $"Endpoint '{settings.Endpoint}' is not a valid absolute URI. Please configure a valid endpoint URL.");
        }

        try
        {
            // Create the OpenAI client with custom endpoint support
            var credential = new ApiKeyCredential(settings.ApiKey);
            var options = new OpenAIClientOptions { Endpoint = endpointUri };
            var openAIClient = new OpenAIClient(credential, options);

            // Validate that the OpenAI client was created successfully
            if (openAIClient == null)
            {
                throw new InvalidOperationException(
                    "Failed to create OpenAI client. The client instance was null after creation.");
            }

            // Get the chat client
            var chatClient = openAIClient.GetChatClient(modelId);

            // Validate that the chat client was created successfully
            if (chatClient == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create chat client for model '{modelId}'. The chat client instance was null after creation.");
            }

            // Convert to IChatClient and add OpenTelemetry support
            var finalClient = chatClient.AsIChatClient().AsBuilder()
                .UseOpenTelemetry()
                .Build();

            // Final validation of the constructed client
            if (finalClient == null)
            {
                throw new InvalidOperationException(
                    "Failed to build final chat client with OpenTelemetry support. The client instance was null after building.");
            }

            _logger.LogInformation(
                "Successfully created chat client for model '{Model}' with endpoint '{Endpoint}'",
                modelId,
                endpointUri);

            return finalClient;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument provided when creating OpenAI client for model '{Model}'", modelId);
            throw new InvalidOperationException($"Invalid configuration for OpenAI client creation: {ex.Message}", ex);
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "Invalid endpoint format '{Endpoint}' when creating OpenAI client", settings.Endpoint);
            throw new InvalidOperationException($"The endpoint '{settings.Endpoint}' is not a valid URI format: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is InvalidOperationException || ex is ArgumentNullException))
        {
            _logger.LogError(ex, "Unexpected error occurred when creating OpenAI client for model '{Model}' with endpoint '{Endpoint}'",
                modelId, settings.Endpoint);
            throw new InvalidOperationException(
                $"Failed to create OpenAI client for model '{modelId}': {ex.Message}", ex);
        }
    }
}