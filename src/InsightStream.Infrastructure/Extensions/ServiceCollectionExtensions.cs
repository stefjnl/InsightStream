using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Factories;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Application.UseCases;
using InsightStream.Infrastructure.Agents;
using InsightStream.Infrastructure.Configuration;
using InsightStream.Infrastructure.Factories;
using InsightStream.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InsightStream.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInsightStreamInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind provider configuration
        services.Configure<ProvidersConfiguration>(options =>
            configuration.GetSection(ProvidersConfiguration.SectionName).Bind(options));
        
        // Bind app configuration
        services.Configure<AppConfiguration>(options =>
            configuration.GetSection(AppConfiguration.SectionName).Bind(options));

        // Add memory cache
        services.AddMemoryCache();

        // Register chat client factory
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();

        // Register YouTube transcript service
        services.AddSingleton<IYouTubeTranscriptService, YouTubeTranscriptService>();

        // Register video cache service
        services.AddSingleton<IVideoCacheService, VideoCacheService>();

        // Register agents (Scoped lifetime)
        services.AddScoped<IYouTubeOrchestrator, YouTubeOrchestratorAgent>();
        services.AddScoped<IContentExtractionAgent, ContentExtractionAgent>();
        services.AddScoped<IAnalysisAgent, AnalysisAgent>();
        services.AddScoped<IQuestionAnsweringAgent, QuestionAnsweringAgent>();

        // Register use cases (Scoped lifetime)
        services.AddScoped<ProcessYouTubeRequestUseCase>();

        return services;
    }
}