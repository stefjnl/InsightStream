using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Domain.Models;
using Microsoft.Extensions.Logging;

namespace InsightStream.Infrastructure.Agents;

/// <summary>
/// Agent responsible for extracting video content from YouTube.
/// </summary>
public class ContentExtractionAgent : IContentExtractionAgent
{
    private readonly IYouTubeTranscriptService _transcriptService;
    private readonly ILogger<ContentExtractionAgent> _logger;

    /// <summary>
    /// Initializes a new instance of the ContentExtractionAgent class.
    /// </summary>
    /// <param name="transcriptService">The YouTube transcript service.</param>
    /// <param name="logger">The logger.</param>
    public ContentExtractionAgent(
        IYouTubeTranscriptService transcriptService,
        ILogger<ContentExtractionAgent> logger)
    {
        _transcriptService = transcriptService ?? throw new ArgumentNullException(nameof(transcriptService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts video metadata and transcript from a YouTube URL.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing video metadata and transcript chunks.</returns>
    public async Task<(VideoMetadata Metadata, IReadOnlyList<TranscriptChunk> Chunks)> ExtractContentAsync(
        string videoUrl, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extracting content from YouTube URL: {VideoUrl}", videoUrl);

            var result = await _transcriptService.ExtractTranscriptAsync(videoUrl, cancellationToken);
            
            _logger.LogInformation(
                "Successfully extracted content for video {VideoId} with {ChunkCount} transcript chunks",
                result.VideoId,
                result.Chunks.Count);

            return (result.Metadata, result.Chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting content from YouTube URL: {VideoUrl}", videoUrl);
            throw;
        }
    }
}