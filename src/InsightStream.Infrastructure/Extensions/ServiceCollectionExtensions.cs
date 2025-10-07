using Microsoft.Extensions.DependencyInjection;

namespace InsightStream.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Infrastructure services will be registered here
        
        return services;
    }
}