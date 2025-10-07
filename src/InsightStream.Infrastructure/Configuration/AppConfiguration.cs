namespace InsightStream.Infrastructure.Configuration;

public sealed class AppConfiguration
{
    public const string SectionName = "DefaultProvider";
    
    public required string DefaultProvider { get; init; }
}