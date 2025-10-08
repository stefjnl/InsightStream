using InsightStream.Domain.Models;

namespace InsightStream.Application.Interfaces.Agents;

/// <summary>
/// Orchestrator agent that coordinates video processing workflows using LLM with tools.
/// </summary>
public interface IYouTubeOrchestrator
{
    /// <summary>
    /// Processes an analyze request for a YouTube video, extracting content and generating a summary.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated summary text for the video.</returns>
    Task<string> ProcessAnalyzeRequestAsync(string videoUrl, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a question request about a specific video, maintaining conversation context.
    /// </summary>
    /// <param name="videoId">The ID of the video to question about.</param>
    /// <param name="question">The question to answer about the video content.</param>
    /// <param name="history">The conversation history for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A streaming response with the answer to the question.</returns>
    IAsyncEnumerable<string> ProcessQuestionRequestAsync(string videoId, string question, List<ConversationMessage> history, CancellationToken cancellationToken);
}