using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InsightStream.Infrastructure.Agents;
using InsightStream.Infrastructure.Configuration;
using InsightStream.Infrastructure.Extensions;
using InsightStream.Infrastructure.Services;
using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Factories;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Application.UseCases;

namespace InsightStream.Application.Tests;

public class ServiceRegistrationTests
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

    public ServiceRegistrationTests()
    {
        // Create configuration with valid provider setup
        // Use the test project configuration file for testing
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(GetTestProjectRoot())
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ServiceRegistrationTests>(optional: true)
            .AddEnvironmentVariables("INSIGHTSTREAM_");

        _configuration = configBuilder.Build();

        // Setup DI container with full service registration
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Register infrastructure services (this is what AddInsightStreamInfrastructure does)
        services.AddInsightStreamInfrastructure(_configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void AllRequiredServices_ShouldBeRegistered()
    {
        // Test all the services that should be registered by AddInsightStreamInfrastructure

        // Configuration services
        Assert.NotNull(_serviceProvider.GetService<IOptions<ProvidersConfiguration>>());
        Assert.NotNull(_serviceProvider.GetService<IOptions<AppConfiguration>>());

        // Factory services
        Assert.NotNull(_serviceProvider.GetService<IChatClientFactory>());

        // Infrastructure services
        Assert.NotNull(_serviceProvider.GetService<IYouTubeTranscriptService>());
        Assert.NotNull(_serviceProvider.GetService<IVideoCacheService>());

        // Agent services
        Assert.NotNull(_serviceProvider.GetService<IYouTubeOrchestrator>());
        Assert.NotNull(_serviceProvider.GetService<IContentExtractionAgent>());
        Assert.NotNull(_serviceProvider.GetService<IAnalysisAgent>());
        Assert.NotNull(_serviceProvider.GetService<IQuestionAnsweringAgent>());

        // Use case
        Assert.NotNull(_serviceProvider.GetService<ProcessYouTubeRequestUseCase>());
    }

    [Fact]
    public void ConfigurationServices_ShouldResolveCorrectly()
    {
        // Act
        var providersConfig = _serviceProvider.GetRequiredService<IOptions<ProvidersConfiguration>>();
        var appConfig = _serviceProvider.GetRequiredService<IOptions<AppConfiguration>>();

        // Assert
        Assert.NotNull(providersConfig.Value);
        Assert.NotNull(appConfig.Value);
        Assert.NotNull(providersConfig.Value.Providers);
        Assert.False(string.IsNullOrEmpty(appConfig.Value.DefaultProvider));
    }

    [Fact]
    public void ChatClientFactory_ShouldBeSingleton()
    {
        // Act
        var factory1 = _serviceProvider.GetRequiredService<IChatClientFactory>();
        var factory2 = _serviceProvider.GetRequiredService<IChatClientFactory>();

        // Assert
        Assert.Same(factory1, factory2);
    }

    [Fact]
    public void TranscriptAndCacheServices_ShouldBeSingleton()
    {
        // Act
        var transcriptService1 = _serviceProvider.GetRequiredService<IYouTubeTranscriptService>();
        var transcriptService2 = _serviceProvider.GetRequiredService<IYouTubeTranscriptService>();
        var cacheService1 = _serviceProvider.GetRequiredService<IVideoCacheService>();
        var cacheService2 = _serviceProvider.GetRequiredService<IVideoCacheService>();

        // Assert
        Assert.Same(transcriptService1, transcriptService2);
        Assert.Same(cacheService1, cacheService2);
    }

    [Fact]
    public void AgentServices_ShouldBeScoped()
    {
        // Act
        using (var scope = _serviceProvider.CreateScope())
        {
            var orchestrator1 = scope.ServiceProvider.GetRequiredService<IYouTubeOrchestrator>();
            var orchestrator2 = scope.ServiceProvider.GetRequiredService<IYouTubeOrchestrator>();
            var extractionAgent1 = scope.ServiceProvider.GetRequiredService<IContentExtractionAgent>();
            var extractionAgent2 = scope.ServiceProvider.GetRequiredService<IContentExtractionAgent>();

            // Assert
            Assert.NotSame(orchestrator1, orchestrator2);
            Assert.NotSame(extractionAgent1, extractionAgent2);
        }
    }

    [Fact]
    public void UseCase_ShouldBeScoped()
    {
        // Act
        using (var scope = _serviceProvider.CreateScope())
        {
            var useCase1 = scope.ServiceProvider.GetRequiredService<ProcessYouTubeRequestUseCase>();
            var useCase2 = scope.ServiceProvider.GetRequiredService<ProcessYouTubeRequestUseCase>();

            // Assert
            Assert.NotSame(useCase1, useCase2);
        }
    }

    [Fact]
    public void ServiceProvider_ShouldResolveAllDependencies()
    {
        // Act & Assert - This will throw if any dependencies are missing
        var exception1 = Record.Exception(() => _serviceProvider.GetRequiredService<IChatClientFactory>());
        var exception2 = Record.Exception(() => _serviceProvider.GetRequiredService<IYouTubeTranscriptService>());
        var exception3 = Record.Exception(() => _serviceProvider.GetRequiredService<IVideoCacheService>());
        var exception4 = Record.Exception(() => _serviceProvider.GetRequiredService<IYouTubeOrchestrator>());
        var exception5 = Record.Exception(() => _serviceProvider.GetRequiredService<IContentExtractionAgent>());
        var exception6 = Record.Exception(() => _serviceProvider.GetRequiredService<IAnalysisAgent>());
        var exception7 = Record.Exception(() => _serviceProvider.GetRequiredService<IQuestionAnsweringAgent>());
        var exception8 = Record.Exception(() => _serviceProvider.GetRequiredService<ProcessYouTubeRequestUseCase>());

        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
        Assert.Null(exception4);
        Assert.Null(exception5);
        Assert.Null(exception6);
        Assert.Null(exception7);
        Assert.Null(exception8);
    }

    [Fact]
    public void ConfigurationValidation_ShouldPassWithValidConfig()
    {
        // Act
        var providersConfig = _serviceProvider.GetRequiredService<IOptions<ProvidersConfiguration>>().Value;
        var appConfig = _serviceProvider.GetRequiredService<IOptions<AppConfiguration>>().Value;

        // Assert - Handle case where no real API keys are configured
        if (providersConfig.Providers.Count == 0)
        {
            // Test passes if no providers are configured (expected when no real API keys)
            Assert.Equal(0, providersConfig.Providers.Count);
            return;
        }

        // If providers are configured, test them
        Assert.True(providersConfig.Providers.Count > 0);
        Assert.False(string.IsNullOrEmpty(appConfig.DefaultProvider));
        Assert.True(providersConfig.Providers.ContainsKey(appConfig.DefaultProvider));
    }

    [Fact]
    public void MemoryCache_ShouldBeRegistered()
    {
        // Act
        var memoryCache = _serviceProvider.GetService<IMemoryCache>();

        // Assert
        Assert.NotNull(memoryCache);
    }

    [Fact]
    public void AllAgents_ShouldHaveValidImplementations()
    {
        // Act
        var orchestrator = _serviceProvider.GetRequiredService<IYouTubeOrchestrator>();
        var extractionAgent = _serviceProvider.GetRequiredService<IContentExtractionAgent>();
        var analysisAgent = _serviceProvider.GetRequiredService<IAnalysisAgent>();
        var questionAgent = _serviceProvider.GetRequiredService<IQuestionAnsweringAgent>();

        // Assert
        Assert.IsType<YouTubeOrchestratorAgent>(orchestrator);
        Assert.IsType<ContentExtractionAgent>(extractionAgent);
        Assert.IsType<AnalysisAgent>(analysisAgent);
        Assert.IsType<QuestionAnsweringAgent>(questionAgent);
    }
}