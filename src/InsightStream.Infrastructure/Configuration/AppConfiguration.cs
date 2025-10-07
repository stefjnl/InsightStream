namespace InsightStream.Infrastructure.Configuration;

public sealed class AppConfiguration
{
    public const string SectionName = "AppConfiguration";
    
    public required string DefaultProvider { get; init; }
}