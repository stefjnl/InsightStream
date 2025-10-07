namespace InsightStream.Application.DTOs;

using InsightStream.Domain.Models;

public sealed record VideoResponse
{
    public required string VideoId { get; init; }
    public required VideoMetadata Metadata { get; init; }
    public required string Summary { get; init; }
}