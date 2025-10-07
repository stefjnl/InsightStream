namespace InsightStream.Application.Interfaces.Agents;

/// <summary>
/// Agent responsible for answering questions about video content.
/// </summary>
public interface IQuestionAnsweringAgent
{
    /// <summary>
    /// Answers a question about the specified video content.
    /// </summary>
    /// <param name="videoId">The ID of the video.</param>
    /// <param name="question">The question to answer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A streaming response with the answer.</returns>
    IAsyncEnumerable<string> AnswerQuestionAsync(string videoId, string question, CancellationToken cancellationToken = default);
}