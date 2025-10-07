using InsightStream.Domain.Models;

namespace InsightStream.Application.Interfaces.Agents;

/// <summary>
/// Agent responsible for extracting video content from YouTube.
/// </summary>
public interface IContentExtractionAgent
{
    /// <summary>
    /// Extracts video metadata and transcript from a YouTube URL.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing video metadata and transcript chunks.</returns>
    Task<(VideoMetadata Metadata, IReadOnlyList<TranscriptChunk> Chunks)> ExtractContentAsync(string videoUrl, CancellationToken cancellationToken = default);
}