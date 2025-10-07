namespace InsightStream.Infrastructure.Configuration;

public sealed class ProvidersConfiguration
{
    public const string SectionName = "Providers";
    
    public Dictionary<string, ProviderSettings> Providers { get; init; } = new();
}

public sealed class ProviderSettings
{
    public required string ApiKey { get; init; }
    public required string Endpoint { get; init; }
    public required List<ModelConfiguration> Models { get; init; }
}

public sealed class ModelConfiguration
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
}