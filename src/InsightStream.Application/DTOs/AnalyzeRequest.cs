namespace InsightStream.Application.DTOs;

public sealed record AnalyzeRequest
{
    public required string VideoUrl { get; init; }
}