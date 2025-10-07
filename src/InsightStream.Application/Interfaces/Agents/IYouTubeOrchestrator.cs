using InsightStream.Domain.Models;

namespace InsightStream.Application.Interfaces.Agents;

/// <summary>
/// Orchestrator agent that coordinates video processing workflows.
/// </summary>
public interface IYouTubeOrchestrator
{
    /// <summary>
    /// Extracts video content including metadata and transcript chunks.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing video metadata and transcript chunks.</returns>
    Task<(VideoMetadata Metadata, IReadOnlyList<TranscriptChunk> Chunks)> ExtractVideoContentAsync(string videoUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a summary for the specified video.
    /// </summary>
    /// <param name="videoId">The ID of the video to summarize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated summary text.</returns>
    Task<string> GenerateSummaryAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a question about the video content.
    /// </summary>
    /// <param name="videoId">The ID of the video.</param>
    /// <param name="question">The question to answer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A streaming response with the answer.</returns>
    IAsyncEnumerable<string> AnswerQuestionAsync(string videoId, string question, CancellationToken cancellationToken = default);
}