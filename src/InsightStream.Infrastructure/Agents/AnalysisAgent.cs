using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Factories;
using InsightStream.Application.Interfaces.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace InsightStream.Infrastructure.Agents;

/// <summary>
/// Agent responsible for analyzing video content and generating summaries.
/// </summary>
public class AnalysisAgent : IAnalysisAgent
{
    private readonly IVideoCacheService _videoCacheService;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<AnalysisAgent> _logger;

    /// <summary>
    /// Initializes a new instance of the AnalysisAgent class.
    /// </summary>
    /// <param name="videoCacheService">The video cache service.</param>
    /// <param name="chatClientFactory">The chat client factory.</param>
    /// <param name="logger">The logger.</param>
    public AnalysisAgent(
        IVideoCacheService videoCacheService,
        IChatClientFactory chatClientFactory,
        ILogger<AnalysisAgent> logger)
    {
        _videoCacheService = videoCacheService ?? throw new ArgumentNullException(nameof(videoCacheService));
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a summary for the specified video content.
    /// </summary>
    /// <param name="videoId">The ID of the video to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated summary text.</returns>
    public async Task<string> GenerateSummaryAsync(
        string videoId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating summary for video: {VideoId}", videoId);

            var videoSession = await _videoCacheService.GetVideoSessionAsync(videoId, cancellationToken);
            if (videoSession == null)
            {
                _logger.LogWarning("Video session not found for video ID: {VideoId}", videoId);
                throw new InvalidOperationException($"Video session not found for video ID: {videoId}");
            }

            // Check if summary already exists
            if (!string.IsNullOrEmpty(videoSession.Summary))
            {
                _logger.LogInformation("Returning existing summary for video: {VideoId}", videoId);
                return videoSession.Summary;
            }

            // Combine transcript chunks for analysis
            var fullTranscript = string.Join(" ", videoSession.Chunks.Select(c => c.Text));
            
            // Create chat client and generate summary
            var chatClient = _chatClientFactory.CreateClient();
            
            var prompt = $"""
                Please provide a comprehensive summary of the following YouTube video transcript.
                The video title is: "{videoSession.Metadata.Title}"
                The channel is: "{videoSession.Metadata.Channel}"
                
                Transcript:
                {fullTranscript}
                
                Please provide a well-structured summary that captures the main points, key insights, and overall message of the video.
                """;

            var messages = new[] { new ChatMessage(ChatRole.User, prompt) };
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            var summary = response.Text ?? string.Empty;

            // Cache the summary
            await _videoCacheService.UpdateSummaryAsync(videoId, summary, cancellationToken);

            _logger.LogInformation("Successfully generated and cached summary for video: {VideoId}", videoId);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for video: {VideoId}", videoId);
            throw;
        }
    }
}