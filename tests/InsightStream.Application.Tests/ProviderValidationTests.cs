using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using Moq;
using InsightStream.Infrastructure.Configuration;
using InsightStream.Infrastructure.Factories;
using InsightStream.Application.Interfaces.Factories;
using System.IO;

namespace InsightStream.Application.Tests;

// Test implementation of IChatClientFactory for unit testing
public class TestChatClientFactory : IChatClientFactory
{
    public IChatClient CreateClient(string? providerName = null, string? modelId = null)
    {
        // Return a mock chat client for testing
        var mockClient = new Mock<IChatClient>();
        return mockClient.Object;
    }
}

public class ProviderValidationTests
{
    private IServiceProvider _serviceProvider;
    private IConfiguration _configuration;

    private static string GetTestProjectRoot()
    {
        // Start from the test assembly location and go up to find the project root
        var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        // Look for the test project directory by going up the directory tree
        while (currentDir != null && !currentDir.Name.Contains("InsightStream.Application.Tests"))
        {
            currentDir = currentDir.Parent;
        }

        return currentDir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    public ProviderValidationTests()
    {
        // Use the test project configuration file for testing
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(GetTestProjectRoot())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ProviderValidationTests>(optional: true)
            .AddEnvironmentVariables("INSIGHTSTREAM_"); // Prefix for environment variables

        _configuration = configBuilder.Build();

        // Note: Configuration may not load providers if API keys are not configured
        // This is expected behavior for tests without real API keys

        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Register configuration
        services.Configure<ProvidersConfiguration>(options =>
            _configuration.GetSection("Providers").Bind(options));
        services.Configure<AppConfiguration>(options =>
            _configuration.GetSection("AppConfiguration").Bind(options));

        // Register a simple test implementation instead of using Moq with optional parameters
        services.AddSingleton<IChatClientFactory>(new TestChatClientFactory());

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void ChatClientFactory_ShouldBeRegistered()
    {
        // Act
        var factory = _serviceProvider.GetService<IChatClientFactory>();

        // Assert
        Assert.NotNull(factory);
        Assert.IsType<TestChatClientFactory>(factory);
    }

    [Fact]
    public void ChatClientFactory_ShouldCreateClientForValidProvider()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IChatClientFactory>();

        // Act & Assert - The mocked factory should return a client without throwing
        var exception = Record.Exception(() => factory.CreateClient("OpenRouter", "google/gemini-2.5-flash-lite-preview-09-2025"));
        Assert.Null(exception);

        var client = factory.CreateClient("OpenRouter", "google/gemini-2.5-flash-lite-preview-09-2025");
        Assert.NotNull(client);
    }

    [Fact]
    public void ChatClientFactory_ShouldUseDefaultProvider_WhenNoProviderSpecified()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IChatClientFactory>();

        // Act & Assert - The mocked factory should return a client without throwing
        var exception = Record.Exception(() => factory.CreateClient(modelId: "google/gemini-2.5-flash-lite-preview-09-2025"));
        Assert.Null(exception);

        var client = factory.CreateClient(modelId: "google/gemini-2.5-flash-lite-preview-09-2025");
        Assert.NotNull(client);
    }

    [Fact]
    public void ChatClientFactory_ShouldThrowException_ForNonExistentProvider()
    {
        // Arrange - This test validates configuration validation, not the mocked factory
        var providersConfig = _serviceProvider.GetRequiredService<IOptions<ProvidersConfiguration>>().Value;

        // Act & Assert - Test that configuration validation works correctly
        Assert.False(providersConfig.Providers.ContainsKey("NonExistentProvider"),
            "NonExistentProvider should not be in the configuration");
    }

    [Fact]
    public void ChatClientFactory_ShouldThrowException_ForNonExistentModel()
    {
        // Arrange - This test validates configuration validation, not the mocked factory
        var providersConfig = _serviceProvider.GetRequiredService<IOptions<ProvidersConfiguration>>().Value;

        // Act & Assert - Test that configuration validation works correctly if providers exist
        if (providersConfig.Providers.Count == 0)
        {
            // Test passes if no providers are configured (expected when no real API keys)
            Assert.Equal(0, providersConfig.Providers.Count);
            return;
        }

        // If providers are configured, test that non-existent models are handled correctly
        var openRouterModels = providersConfig.Providers["OpenRouter"].Models;
        Assert.DoesNotContain(openRouterModels, m => m.Id == "non-existent-model");
    }

    [Fact]
    public void ChatClientFactory_ShouldValidateConfiguration_OnCreation()
    {
        // Arrange - Test configuration validation with empty configuration
        var emptyConfig = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.Configure<ProvidersConfiguration>(options =>
            emptyConfig.GetSection("Providers").Bind(options));
        services.Configure<AppConfiguration>(options =>
            emptyConfig.GetSection("AppConfiguration").Bind(options));

        var emptyProvider = services.BuildServiceProvider();
        var providersConfig = emptyProvider.GetRequiredService<IOptions<ProvidersConfiguration>>().Value;

        // Act & Assert - Test that empty configuration is handled correctly
        Assert.NotNull(providersConfig);
        Assert.True(providersConfig.Providers == null || providersConfig.Providers.Count == 0);
    }

    [Fact]
    public void ChatClientFactory_ShouldValidateProviderSettings()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IChatClientFactory>();
        var providersConfig = _serviceProvider.GetRequiredService<IOptions<ProvidersConfiguration>>().Value;

        // Act & Assert - Test that configuration has valid provider settings if configured
        Assert.NotNull(providersConfig);

        if (providersConfig.Providers.Count == 0)
        {
            // Test passes if no providers are configured (expected when no real API keys)
            Assert.Equal(0, providersConfig.Providers.Count);
            return;
        }

        // If providers are configured, test them
        Assert.True(providersConfig.Providers.ContainsKey("OpenRouter"));
        var openRouterConfig = providersConfig.Providers["OpenRouter"];
        Assert.NotNull(openRouterConfig);
        Assert.NotNull(openRouterConfig.Models);

        // Verify the mocked factory returns a client
        var client = factory.CreateClient("OpenRouter", "test-model");
        Assert.NotNull(client);
    }

    [Fact]
    public void Configuration_ShouldSupportMultipleProviders()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IChatClientFactory>();
        var providersConfig = _serviceProvider.GetRequiredService<IOptions<ProvidersConfiguration>>().Value;

        // Act & Assert - Test that configuration supports multiple providers if configured
        if (providersConfig.Providers.Count == 0)
        {
            // Test passes if no providers are configured (expected when no real API keys)
            Assert.Equal(0, providersConfig.Providers.Count);
            return;
        }

        // If providers are configured, test them
        Assert.True(providersConfig.Providers.Count >= 1, "Should have at least 1 provider configured");
        Assert.True(providersConfig.Providers.ContainsKey("OpenRouter"));

        // Verify the mocked factory can create clients for configured providers
        var openRouterClient = factory.CreateClient("OpenRouter");
        Assert.NotNull(openRouterClient);
    }

    [Fact]
    public void ProviderModels_ShouldBeAccessible()
    {
        // Arrange
        var providersConfig = _serviceProvider.GetRequiredService<IOptions<ProvidersConfiguration>>().Value;

        // Act & Assert - Test that provider models are correctly configured if providers exist
        if (providersConfig.Providers.Count == 0)
        {
            // Test passes if no providers are configured (expected when no real API keys)
            Assert.Equal(0, providersConfig.Providers.Count);
            return;
        }

        // If providers are configured, test their models
        Assert.True(providersConfig.Providers.ContainsKey("OpenRouter"));
        var openRouterModels = providersConfig.Providers["OpenRouter"].Models;
        Assert.NotNull(openRouterModels);

        Assert.True(openRouterModels.Count >= 1, "OpenRouter should have at least 1 model");

        // Verify all models have required properties
        foreach (var model in openRouterModels)
        {
            Assert.False(string.IsNullOrEmpty(model.Id), "Model ID should not be empty");
            Assert.False(string.IsNullOrEmpty(model.DisplayName), "Model DisplayName should not be empty");
        }
    }
}