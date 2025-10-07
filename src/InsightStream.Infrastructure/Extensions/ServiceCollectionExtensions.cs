using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InsightStream.Infrastructure.Configuration;

namespace InsightStream.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds InsightStream infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInsightStreamInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate LLM provider configuration
        services.Configure<LlmProviderConfiguration>(options =>
            configuration.GetSection(LlmProviderConfiguration.SectionName).Bind(options));
        
        services.AddOptions<LlmProviderConfiguration>()
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // TODO: Register IChatClient (Phase 2)
        // TODO: Register agents (Phase 3)
        // TODO: Register services (Phase 2)
        // TODO: Register use cases (Phase 4)
        
        return services;
    }
}