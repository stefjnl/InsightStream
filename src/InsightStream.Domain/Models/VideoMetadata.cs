namespace InsightStream.Domain.Models;

public sealed record VideoMetadata
{
    public required string Title { get; init; }
    public required string Channel { get; init; }
    public required TimeSpan Duration { get; init; }
}