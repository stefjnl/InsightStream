using System.ComponentModel.DataAnnotations;

namespace InsightStream.Infrastructure.Configuration;

public sealed class LlmProviderConfiguration
{
    public const string SectionName = "LlmProvider";
    
    [Required]
    public required string Type { get; init; }
    
    [Required(ErrorMessage = "LLM provider API key is required")]
    public required string ApiKey { get; init; }
    
    [Required, Url]
    public required string Endpoint { get; init; }
    
    [Required]
    public required string ModelId { get; init; }
}