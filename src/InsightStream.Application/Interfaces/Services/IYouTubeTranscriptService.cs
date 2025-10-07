using InsightStream.Domain.Models;

namespace InsightStream.Application.Interfaces.Services;

public interface IYouTubeTranscriptService
{
    Task<VideoExtractionResult> ExtractTranscriptAsync(
        string videoUrl, 
        CancellationToken cancellationToken = default);
}

public sealed record VideoExtractionResult
{
    public required string VideoId { get; init; }
    public required VideoMetadata Metadata { get; init; }
    public required IReadOnlyList<TranscriptChunk> Chunks { get; init; }
}