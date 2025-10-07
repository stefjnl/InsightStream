using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using InsightStream.Application.UseCases;
using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Infrastructure.Extensions;
using InsightStream.Domain.Models;
using Moq;

namespace InsightStream.Application.Tests;

public class ProcessYouTubeRequestUseCaseTests
{
    [Fact]
    public void UseCase_ShouldBeRegisteredInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add mocks for missing dependencies
        var mockOrchestrator = new Mock<IYouTubeOrchestrator>();
        var mockCacheService = new Mock<IVideoCacheService>();
        
        services.AddSingleton(mockOrchestrator.Object);
        services.AddSingleton(mockCacheService.Object);
        
        // Act
        services.AddInsightStreamInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var useCase = serviceProvider.GetService<ProcessYouTubeRequestUseCase>();
        Assert.NotNull(useCase);
    }
    
    [Fact]
    public void UseCase_CanBeInstantiatedWithMocks()
    {
        // Arrange
        var mockOrchestrator = new Mock<IYouTubeOrchestrator>();
        var mockCacheService = new Mock<IVideoCacheService>();
        var mockLogger = new Mock<ILogger<ProcessYouTubeRequestUseCase>>();
        
        // Act
        var useCase = new ProcessYouTubeRequestUseCase(
            mockOrchestrator.Object,
            mockCacheService.Object,
            mockLogger.Object);
        
        // Assert
        Assert.NotNull(useCase);
    }
}