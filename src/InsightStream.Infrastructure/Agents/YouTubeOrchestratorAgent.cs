using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Domain.Models;
using Microsoft.Extensions.Logging;

namespace InsightStream.Infrastructure.Agents;

/// <summary>
/// Orchestrator agent that coordinates video processing workflows.
/// </summary>
public class YouTubeOrchestratorAgent : IYouTubeOrchestrator
{
    private readonly IContentExtractionAgent _contentExtractionAgent;
    private readonly IAnalysisAgent _analysisAgent;
    private readonly IQuestionAnsweringAgent _questionAnsweringAgent;
    private readonly IVideoCacheService _videoCacheService;
    private readonly ILogger<YouTubeOrchestratorAgent> _logger;

    /// <summary>
    /// Initializes a new instance of the YouTubeOrchestratorAgent class.
    /// </summary>
    /// <param name="contentExtractionAgent">The content extraction agent.</param>
    /// <param name="analysisAgent">The analysis agent.</param>
    /// <param name="questionAnsweringAgent">The question answering agent.</param>
    /// <param name="videoCacheService">The video cache service.</param>
    /// <param name="logger">The logger.</param>
    public YouTubeOrchestratorAgent(
        IContentExtractionAgent contentExtractionAgent,
        IAnalysisAgent analysisAgent,
        IQuestionAnsweringAgent questionAnsweringAgent,
        IVideoCacheService videoCacheService,
        ILogger<YouTubeOrchestratorAgent> logger)
    {
        _contentExtractionAgent = contentExtractionAgent ?? throw new ArgumentNullException(nameof(contentExtractionAgent));
        _analysisAgent = analysisAgent ?? throw new ArgumentNullException(nameof(analysisAgent));
        _questionAnsweringAgent = questionAnsweringAgent ?? throw new ArgumentNullException(nameof(questionAnsweringAgent));
        _videoCacheService = videoCacheService ?? throw new ArgumentNullException(nameof(videoCacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts video content including metadata and transcript chunks.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing video metadata and transcript chunks.</returns>
    public async Task<(VideoMetadata Metadata, IReadOnlyList<TranscriptChunk> Chunks)> ExtractVideoContentAsync(
        string videoUrl, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extracting video content from URL: {VideoUrl}", videoUrl);

            // Extract content using the content extraction agent
            var (metadata, chunks) = await _contentExtractionAgent.ExtractContentAsync(videoUrl, cancellationToken);

            // Extract video ID from the result (we'll need to modify this based on actual implementation)
            // For now, we'll assume the video URL contains the ID or we can extract it from metadata
            var videoId = ExtractVideoIdFromUrl(videoUrl);

            // Check if video session already exists
            var existingSession = await _videoCacheService.GetVideoSessionAsync(videoId, cancellationToken);
            if (existingSession == null)
            {
                // Create new video session
                var newSession = new VideoSession
                {
                    VideoId = videoId,
                    Metadata = metadata,
                    Chunks = chunks,
                    ConversationHistory = new List<ConversationMessage>()
                };

                await _videoCacheService.SetVideoSessionAsync(newSession, cancellationToken);
                _logger.LogInformation("Created new video session for video: {VideoId}", videoId);
            }
            else
            {
                _logger.LogInformation("Video session already exists for video: {VideoId}", videoId);
            }

            _logger.LogInformation(
                "Successfully extracted video content for {VideoId}: {Title} with {ChunkCount} chunks",
                videoId,
                metadata.Title,
                chunks.Count);

            return (metadata, chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting video content from URL: {VideoUrl}", videoUrl);
            throw;
        }
    }

    /// <summary>
    /// Generates a summary for the specified video.
    /// </summary>
    /// <param name="videoId">The ID of the video to summarize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated summary text.</returns>
    public async Task<string> GenerateSummaryAsync(
        string videoId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating summary for video: {VideoId}", videoId);

            // Check if video exists
            var videoExists = await _videoCacheService.VideoExistsAsync(videoId, cancellationToken);
            if (!videoExists)
            {
                _logger.LogWarning("Video not found for ID: {VideoId}", videoId);
                throw new InvalidOperationException($"Video not found for ID: {videoId}");
            }

            // Generate summary using the analysis agent
            var summary = await _analysisAgent.GenerateSummaryAsync(videoId, cancellationToken);

            _logger.LogInformation("Successfully generated summary for video: {VideoId}", videoId);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for video: {VideoId}", videoId);
            throw;
        }
    }

    /// <summary>
    /// Answers a question about the video content.
    /// </summary>
    /// <param name="videoId">The ID of the video.</param>
    /// <param name="question">The question to answer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A streaming response with the answer.</returns>
    public async IAsyncEnumerable<string> AnswerQuestionAsync(
        string videoId,
        string question,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Answering question for video: {VideoId}, Question: {Question}", videoId, question);

        // Check if video exists
        var videoExists = await _videoCacheService.VideoExistsAsync(videoId, cancellationToken);
        if (!videoExists)
        {
            _logger.LogWarning("Video not found for ID: {VideoId}", videoId);
            throw new InvalidOperationException($"Video not found for ID: {videoId}");
        }

        // Answer question using the question answering agent
        await foreach (var response in _questionAnsweringAgent.AnswerQuestionAsync(videoId, question, cancellationToken))
        {
            yield return response;
        }

        _logger.LogInformation("Successfully answered question for video: {VideoId}", videoId);
    }

    /// <summary>
    /// Extracts video ID from YouTube URL.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL.</param>
    /// <returns>The video ID.</returns>
    private static string ExtractVideoIdFromUrl(string videoUrl)
    {
        try
        {
            var uri = new Uri(videoUrl);
            
            // Handle standard YouTube URLs with v parameter
            if (uri.Query.Contains("v="))
            {
                var query = uri.Query.Substring(1); // Remove '?'
                var pairs = query.Split('&');
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2 && keyValue[0] == "v")
                    {
                        return keyValue[1];
                    }
                }
            }

            // Handle youtu.be short URLs
            if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                return uri.AbsolutePath.Trim('/');
            }

            // Handle embedded URLs
            if (uri.AbsolutePath.Contains("/embed/"))
            {
                var parts = uri.AbsolutePath.Split('/');
                var embedIndex = Array.IndexOf(parts, "embed");
                if (embedIndex >= 0 && embedIndex + 1 < parts.Length)
                {
                    return parts[embedIndex + 1];
                }
            }
        }
        catch
        {
            // If URL parsing fails, fall back to hash
        }

        // Fallback - use a hash of the URL
        return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(videoUrl))[..11];
    }
}