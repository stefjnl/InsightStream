using System.ClientModel;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using OpenAI;
using InsightStream.Application.Interfaces.Factories;
using InsightStream.Infrastructure.Configuration;
using InsightStream.Infrastructure.Extensions;

namespace InsightStream.Application.Tests;

/// <summary>
/// Integration tests for OpenRouter API connectivity.
/// These tests make actual HTTP requests to OpenRouter's API endpoints.
/// </summary>
public class OpenRouterIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ProvidersConfiguration _providersConfig;
    private readonly ILogger<OpenRouterIntegrationTests> _logger;
    private readonly HttpClient _httpClient;
    private bool _disposed = false;

    public OpenRouterIntegrationTests()
    {
        // Create configuration with test settings
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(GetTestProjectRoot())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddUserSecrets<OpenRouterIntegrationTests>(optional: true)
            .AddEnvironmentVariables("INSIGHTSTREAM_");

        _configuration = configBuilder.Build();

        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Register infrastructure services
        services.AddInsightStreamInfrastructure(_configuration);

        // Register HttpClient for direct API calls
        services.AddHttpClient();

        _serviceProvider = services.BuildServiceProvider();
        _chatClientFactory = _serviceProvider.GetRequiredService<IChatClientFactory>();
        _providersConfig = _serviceProvider.GetRequiredService<IOptions<ProvidersConfiguration>>().Value;
        _logger = _serviceProvider.GetRequiredService<ILogger<OpenRouterIntegrationTests>>();
        _httpClient = _serviceProvider.GetRequiredService<HttpClient>();

        // Debug: Log the raw configuration
        _logger.LogInformation("=== Raw Configuration Debug ===");
        var providersSection = _configuration.GetSection("Providers");
        _logger.LogInformation("Providers section exists: {Exists}", providersSection.Exists());
        var openRouterSection = providersSection.GetSection("OpenRouter");
        _logger.LogInformation("OpenRouter section exists: {Exists}", openRouterSection.Exists());
        if (openRouterSection.Exists())
        {
            _logger.LogInformation("OpenRouter ApiKey: {ApiKey}", openRouterSection["ApiKey"]);
            _logger.LogInformation("OpenRouter Endpoint: {Endpoint}", openRouterSection["Endpoint"]);
        }
        _logger.LogInformation("=== End Raw Configuration Debug ===");

        // Debug: Test manual configuration binding
        _logger.LogInformation("=== Manual Binding Test ===");
        var manualConfig = new ProvidersConfiguration();
        providersSection.Bind(manualConfig);
        _logger.LogInformation("Manual binding - Providers count: {Count}", manualConfig.Providers?.Count ?? 0);
        if (manualConfig.Providers != null && manualConfig.Providers.ContainsKey("OpenRouter"))
        {
            var manualOpenRouter = manualConfig.Providers["OpenRouter"];
            _logger.LogInformation("Manual binding - OpenRouter ApiKey: {ApiKey}",
                string.IsNullOrEmpty(manualOpenRouter.ApiKey) ? "NULL" : manualOpenRouter.ApiKey.Substring(0, Math.Min(10, manualOpenRouter.ApiKey.Length)) + "...");
        }
        _logger.LogInformation("=== End Manual Binding Test ===");
    }

    private static string GetTestProjectRoot()
    {
        var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (currentDir != null && !currentDir.Name.Contains("InsightStream.Application.Tests"))
        {
            currentDir = currentDir.Parent;
        }
        return currentDir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    [Fact]
    public async Task Authentication_WithValidApiKey_ShouldSucceed()
    {
        // Arrange
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured with valid API key. Skipping authentication test.");
            return;
        }

        var openRouterConfig = _providersConfig.Providers["OpenRouter"];
        var endpoint = $"{openRouterConfig.Endpoint.TrimEnd('/')}/models";

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Authorization", $"Bearer {openRouterConfig.ApiKey}");
        request.Headers.Add("HTTP-Referer", "https://localhost:5000");
        request.Headers.Add("X-Title", "InsightStream Integration Tests");

        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, 
            $"Authentication failed with status code {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify the response contains expected model data structure
        var jsonDoc = JsonDocument.Parse(content);
        Assert.True(jsonDoc.RootElement.TryGetProperty("data", out var dataElement));
        Assert.True(dataElement.GetArrayLength() > 0, "No models returned from OpenRouter API");
    }

    [Fact]
    public async Task ModelAvailability_ConfiguredModels_ShouldBeAvailable()
    {
        // Arrange
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping model availability test.");
            return;
        }

        var openRouterConfig = _providersConfig.Providers["OpenRouter"];
        var endpoint = $"{openRouterConfig.Endpoint.TrimEnd('/')}/models";

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Authorization", $"Bearer {openRouterConfig.ApiKey}");
        request.Headers.Add("HTTP-Referer", "https://localhost:5000");
        request.Headers.Add("X-Title", "InsightStream Integration Tests");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var availableModels = new List<string>();

        if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
        {
            foreach (var model in dataElement.EnumerateArray())
            {
                if (model.TryGetProperty("id", out var idElement))
                {
                    availableModels.Add(idElement.GetString() ?? string.Empty);
                }
            }
        }

        // Assert
        Assert.NotEmpty(availableModels);

        foreach (var configuredModel in openRouterConfig.Models)
        {
            Assert.Contains(configuredModel.Id, availableModels);
        }
    }

    [Fact]
    public async Task BasicChatCompletion_WithValidRequest_ShouldReturnResponse()
    {
        // Arrange
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping chat completion test.");
            return;
        }

        var testModel = _providersConfig.Providers["OpenRouter"].Models.First().Id;
        var chatClient = _chatClientFactory.CreateClient("OpenRouter", testModel);
        var message = new ChatMessage(ChatRole.User, "What is 2 + 2? Answer with just the number.");

        // Act
        var response = await chatClient.GetResponseAsync([message]);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Text ?? string.Empty);
        
        // Verify the response contains a reasonable answer
        var content = response.Text?.ToLowerInvariant() ?? string.Empty;
        Assert.True(content.Contains("4") || content.Contains("four"),
            $"Expected response to contain '4' or 'four', but got: {response.Text}");
    }

    [Fact]
    public async Task ChatCompletion_WithSystemMessage_ShouldFollowInstructions()
    {
        // Arrange
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping system message test.");
            return;
        }

        var testModel = _providersConfig.Providers["OpenRouter"].Models.First().Id;
        var chatClient = _chatClientFactory.CreateClient("OpenRouter", testModel);
        
        var messages = new[]
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant. Always respond with 'Hello, World!' and nothing else."),
            new ChatMessage(ChatRole.User, "Tell me about the weather.")
        };

        // Act
        var response = await chatClient.GetResponseAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Hello, World!", response.Text?.Trim() ?? string.Empty);
    }

    [Fact]
    public async Task ErrorHandling_WithInvalidApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        if (!_providersConfig.Providers.ContainsKey("OpenRouter"))
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping invalid API key test.");
            return;
        }

        var openRouterConfig = _providersConfig.Providers["OpenRouter"];
        var endpoint = $"{openRouterConfig.Endpoint.TrimEnd('/')}/models";
        var invalidApiKey = "sk-invalid-key-for-testing";

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Authorization", $"Bearer {invalidApiKey}");
        request.Headers.Add("HTTP-Referer", "https://localhost:5000");
        request.Headers.Add("X-Title", "InsightStream Integration Tests");

        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("unauthorized", content.ToLowerInvariant());
    }

    [Fact]
    public async Task ErrorHandling_WithInvalidModel_ShouldReturnError()
    {
        // Arrange
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping invalid model test.");
            return;
        }

        var invalidModel = "invalid/model/does-not-exist";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.Run(() => _chatClientFactory.CreateClient("OpenRouter", invalidModel)));

        Assert.Contains("not configured for provider", exception.Message);
    }

    [Fact]
    public async Task ErrorHandling_WithInvalidEndpoint_ShouldFailGracefully()
    {
        // Arrange
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping invalid endpoint test.");
            return;
        }

        // Create a temporary configuration with invalid endpoint
        var invalidConfig = new ProviderSettings
        {
            ApiKey = _providersConfig.Providers["OpenRouter"].ApiKey,
            Endpoint = "https://invalid-endpoint-that-does-not-exist.com/api/v1/",
            Models = _providersConfig.Providers["OpenRouter"].Models
        };

        // Act
        var exception = await Assert.ThrowsAnyAsync<Exception>(
        async () =>
        {
            var credential = new ApiKeyCredential(invalidConfig.ApiKey);
            var options = new OpenAIClientOptions { Endpoint = new Uri(invalidConfig.Endpoint) };
            var openAIClient = new OpenAIClient(credential, options);
            var chatClient = openAIClient.GetChatClient(invalidConfig.Models.First().Id);
            await chatClient.AsIChatClient().GetResponseAsync([new ChatMessage(ChatRole.User, "test")]);
        });

        // Assert
        Assert.True(exception is HttpRequestException || exception is InvalidOperationException);
    }

    [Fact]
    public async Task ConcurrentRequests_MultipleChatCompletions_ShouldHandleCorrectly()
    {
        // Arrange
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping concurrent requests test.");
            return;
        }

        var testModel = _providersConfig.Providers["OpenRouter"].Models.First().Id;
        var chatClient = _chatClientFactory.CreateClient("OpenRouter", testModel);
        var tasks = new List<Task<ChatResponse>>();

        // Act - Create multiple concurrent requests
        for (int i = 0; i < 5; i++)
        {
            var message = new ChatMessage(ChatRole.User, $"What is {i} + {i}? Answer with just the number.");
            tasks.Add(chatClient.GetResponseAsync([message]));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, responses.Length);
        foreach (var response in responses)
        {
            Assert.NotNull(response);
            Assert.NotEmpty(response.Text ?? string.Empty);
        }
    }

    [Fact]
    public async Task ChatCompletion_WithLongMessage_ShouldHandleCorrectly()
    {
        // Arrange
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping long message test.");
            return;
        }

        var testModel = _providersConfig.Providers["OpenRouter"].Models.First().Id;
        var chatClient = _chatClientFactory.CreateClient("OpenRouter", testModel);
        
        // Create a long message (approximately 1000 words)
        var longText = string.Join(" ", Enumerable.Repeat("This is a test sentence. ", 200));
        var message = new ChatMessage(ChatRole.User, $"Please summarize this text in one sentence: {longText}");

        // Act
        var response = await chatClient.GetResponseAsync([message]);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Text ?? string.Empty);
        
        // The summary should be significantly shorter than the original text
        Assert.True((response.Text?.Length ?? 0) < longText.Length / 2);
    }

    [Fact]
    public void ChatClientFactory_WithValidConfiguration_ShouldCreateClient()
    {
        // Debug configuration state
        LogConfigurationState();
        
        // Arrange - Temporarily bypass the configuration check to force the test
        if (!IsOpenRouterConfigured())
        {
            _logger.LogWarning("OpenRouter is not configured. Skipping factory test.");
            // For debugging, let's still try to run the test
            // return;
        }

        // Act
        var exception = Record.Exception(() =>
        {
            var client = _chatClientFactory.CreateClient("OpenRouter");
            Assert.NotNull(client);
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task DirectApiCall_WithRealApiKey_ShouldSucceed()
    {
        // Arrange - Use the real API key directly
        var apiKey = "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";
        var endpoint = "https://openrouter.ai/api/v1/models";

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Headers.Add("HTTP-Referer", "https://localhost:5000");
        request.Headers.Add("X-Title", "InsightStream Integration Tests");

        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode,
            $"Direct API call failed with status code {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify the response contains expected model data structure
        var jsonDoc = JsonDocument.Parse(content);
        Assert.True(jsonDoc.RootElement.TryGetProperty("data", out var dataElement));
        Assert.True(dataElement.GetArrayLength() > 0, "No models returned from OpenRouter API");
        
        _logger.LogInformation("Direct API call successful! Retrieved {Count} models", dataElement.GetArrayLength());
    }

    [Fact]
    public void ChatClientFactory_WithInvalidProvider_ShouldThrowException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _chatClientFactory.CreateClient("InvalidProvider"));

        Assert.Contains("No providers are configured", exception.Message);
    }

    private bool IsOpenRouterConfigured()
    {
        try
        {
            return _providersConfig.Providers.ContainsKey("OpenRouter") &&
                   !string.IsNullOrEmpty(_providersConfig.Providers["OpenRouter"].ApiKey) &&
                   !_providersConfig.Providers["OpenRouter"].ApiKey.Contains("*** PLACEHOLDER");
        }
        catch
        {
            return false;
        }
    }

    // Debug method to check configuration
    private void LogConfigurationState()
    {
        try
        {
            _logger.LogInformation("=== Configuration Debug Info ===");
            _logger.LogInformation("Providers count: {Count}", _providersConfig.Providers.Count);
            
            if (_providersConfig.Providers.ContainsKey("OpenRouter"))
            {
                var config = _providersConfig.Providers["OpenRouter"];
                _logger.LogInformation("OpenRouter found:");
                _logger.LogInformation("  ApiKey: {ApiKey}", config.ApiKey);
                _logger.LogInformation("  Endpoint: {Endpoint}", config.Endpoint);
                _logger.LogInformation("  Models count: {Count}", config.Models.Count);
                _logger.LogInformation("  IsConfigured: {IsConfigured}", IsOpenRouterConfigured());
            }
            else
            {
                _logger.LogInformation("OpenRouter NOT found in providers");
                _logger.LogInformation("Available providers: {Providers}", string.Join(", ", _providersConfig.Providers.Keys));
            }
            _logger.LogInformation("=== End Configuration Debug Info ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging configuration state");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            if (_serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
            _disposed = true;
        }
    }
}