namespace InsightStream.Application.Interfaces.Agents;

/// <summary>
/// Agent responsible for analyzing video content and generating summaries.
/// </summary>
public interface IAnalysisAgent
{
    /// <summary>
    /// Generates a summary for the specified video content.
    /// </summary>
    /// <param name="videoId">The ID of the video to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated summary text.</returns>
    Task<string> GenerateSummaryAsync(string videoId, CancellationToken cancellationToken = default);
}