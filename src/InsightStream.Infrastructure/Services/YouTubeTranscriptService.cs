using System.Text;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Exceptions;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Domain.Models;
using InsightStream.Domain.Constants;

namespace InsightStream.Infrastructure.Services;

/// <summary>
/// Service for extracting YouTube video transcripts with intelligent chunking.
/// </summary>
public sealed class YouTubeTranscriptService : IYouTubeTranscriptService
{
    private readonly YoutubeClient _youtubeClient;
    private readonly ILogger<YouTubeTranscriptService> _logger;

    /// <summary>
    /// Initializes a new instance of the YouTubeTranscriptService class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public YouTubeTranscriptService(ILogger<YouTubeTranscriptService> logger)
    {
        _youtubeClient = new YoutubeClient();
        _logger = logger;
    }

    /// <summary>
    /// Extracts transcript from a YouTube video and chunks it intelligently.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The video extraction result containing metadata and transcript chunks.</returns>
    public async Task<VideoExtractionResult> ExtractTranscriptAsync(
        string videoUrl, 
        CancellationToken cancellationToken = default)
    {
        // 1. Extract and validate video ID
        var videoId = ExtractVideoId(videoUrl);
        
        // 2. Fetch video metadata
        var metadata = await FetchMetadataAsync(videoId, cancellationToken);
        
        // 3. Fetch transcript with timestamps
        var captions = await FetchTranscriptAsync(videoId, cancellationToken);
        
        // 4. Chunk transcript with overlap
        var chunks = ChunkTranscript(captions);
        
        // 5. Return result
        return new VideoExtractionResult
        {
            VideoId = videoId,
            Metadata = metadata,
            Chunks = chunks
        };
    }

    /// <summary>
    /// Extracts and validates the video ID from a YouTube URL.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL.</param>
    /// <returns>The extracted video ID.</returns>
    /// <exception cref="ArgumentException">Thrown when the URL is invalid.</exception>
    private string ExtractVideoId(string videoUrl)
    {
        try
        {
            var videoId = YoutubeExplode.Videos.VideoId.TryParse(videoUrl);
            if (videoId is null)
            {
                throw new ArgumentException(
                    "❌ Invalid YouTube URL. Please provide a valid YouTube video link.",
                    nameof(videoUrl));
            }
            return videoId.Value;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException(
                "❌ Invalid YouTube URL. Please provide a valid YouTube video link.", 
                nameof(videoUrl), 
                ex);
        }
    }

    /// <summary>
    /// Fetches video metadata from YouTube.
    /// </summary>
    /// <param name="videoId">The video ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The video metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the video is unavailable.</exception>
    private async Task<VideoMetadata> FetchMetadataAsync(
        YoutubeExplode.Videos.VideoId videoId,
        CancellationToken cancellationToken)
    {
        try
        {
            var video = await _youtubeClient.Videos.GetAsync(videoId, cancellationToken);
            
            return new VideoMetadata
            {
                Title = video.Title,
                Channel = video.Author.ChannelTitle,
                Duration = video.Duration ?? TimeSpan.Zero
            };
        }
        catch (VideoUnavailableException ex)
        {
            _logger.LogWarning(ex, "Video {VideoId} is unavailable", videoId);
            throw new InvalidOperationException(
                $"❌ Video is unavailable. It may be private, deleted, age-restricted, or region-locked.", 
                ex);
        }
    }

    /// <summary>
    /// Fetches transcript captions from YouTube.
    /// </summary>
    /// <param name="videoId">The video ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of transcript captions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when captions are not available.</exception>
    private async Task<List<ClosedCaption>> FetchTranscriptAsync(
        YoutubeExplode.Videos.VideoId videoId,
        CancellationToken cancellationToken)
    {
        try
        {
            var trackManifest = await _youtubeClient.Videos.ClosedCaptions
                .GetManifestAsync(videoId, cancellationToken);
            
            // Prefer English, fallback to first available
            var trackInfo = trackManifest.Tracks
                .FirstOrDefault(t => t.Language.Code == "en") 
                ?? trackManifest.Tracks.FirstOrDefault();
            
            if (trackInfo is null)
            {
                throw new InvalidOperationException(
                    "❌ No captions available for this video. The video creator has not enabled captions.");
            }
            
            var track = await _youtubeClient.Videos.ClosedCaptions
                .GetAsync(trackInfo, cancellationToken);
            
            return track.Captions.ToList();
        }
        catch (VideoUnavailableException ex)
        {
            throw new InvalidOperationException(
                "❌ Video is unavailable. It may be private, deleted, age-restricted, or region-locked.", 
                ex);
        }
    }

    /// <summary>
    /// Chunks the transcript captions with intelligent overlap.
    /// </summary>
    /// <param name="captions">The list of transcript captions.</param>
    /// <returns>The list of transcript chunks.</returns>
    private IReadOnlyList<TranscriptChunk> ChunkTranscript(List<ClosedCaption> captions)
    {
        if (captions.Count == 0)
        {
            return Array.Empty<TranscriptChunk>();
        }

        var chunks = new List<TranscriptChunk>();
        
        // Calculate optimal StringBuilder capacity based on transcript characteristics
        var estimatedChunkCapacity = CalculateOptimalChunkCapacity(captions);
        var currentChunk = new StringBuilder(estimatedChunkCapacity);
        
        var chunkStartTime = captions[0].Offset;
        var chunkStartIndex = 0;
        var currentCharCount = 0;

        for (int i = 0; i < captions.Count; i++)
        {
            var caption = captions[i];
            var captionText = caption.Text + " ";
            var captionLength = captionText.Length;

            // If adding this caption exceeds chunk size, finalize current chunk
            if (currentCharCount + captionLength > TranscriptConstants.ChunkSizeCharacters 
                && currentCharCount > 0)
            {
                // Create chunk
                chunks.Add(new TranscriptChunk
                {
                    Text = currentChunk.ToString().Trim(),
                    StartTime = chunkStartTime,
                    EndTime = caption.Offset,
                    ChunkIndex = chunks.Count
                });

                // Calculate overlap start point
                var overlapStartIndex = Math.Max(
                    chunkStartIndex,
                    i - CalculateOverlapCaptionCount(captions, i));

                // Start new chunk with overlap - estimate capacity for overlap chunk
                var overlapCaptionCount = i - overlapStartIndex + 1;
                var estimatedOverlapCapacity = EstimateOverlapCapacity(captions, overlapStartIndex, i);
                
                currentChunk.Clear();
                if (currentChunk.Capacity < estimatedOverlapCapacity)
                {
                    currentChunk.Capacity = estimatedOverlapCapacity;
                }
                
                chunkStartIndex = overlapStartIndex;
                chunkStartTime = captions[overlapStartIndex].Offset;
                currentCharCount = 0;

                // Add overlap captions
                for (int j = overlapStartIndex; j <= i; j++)
                {
                    var overlapText = captions[j].Text + " ";
                    currentChunk.Append(overlapText);
                    currentCharCount += overlapText.Length;
                }
            }
            else
            {
                currentChunk.Append(captionText);
                currentCharCount += captionLength;
            }
        }

        // Add final chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(new TranscriptChunk
            {
                Text = currentChunk.ToString().Trim(),
                StartTime = chunkStartTime,
                EndTime = captions[^1].Offset + captions[^1].Duration,
                ChunkIndex = chunks.Count
            });
        }

        _logger.LogInformation(
            "Chunked transcript into {ChunkCount} chunks (avg {AvgSize} chars per chunk)",
            chunks.Count,
            chunks.Average(c => c.Text.Length));

        return chunks;
    }

    /// <summary>
    /// Calculates the number of captions needed for the overlap.
    /// </summary>
    /// <param name="captions">The list of captions.</param>
    /// <param name="currentIndex">The current caption index.</param>
    /// <returns>The number of captions for overlap.</returns>
    private int CalculateOverlapCaptionCount(List<ClosedCaption> captions, int currentIndex)
    {
        var overlapChars = 0;
        var count = 0;
        
        for (int i = currentIndex - 1; i >= 0 && overlapChars < TranscriptConstants.ChunkOverlapCharacters; i--)
        {
            overlapChars += captions[i].Text.Length + 1; // +1 for space
            count++;
        }
        
        return count;
    }

    /// <summary>
    /// Estimates the StringBuilder capacity needed for overlap captions.
    /// </summary>
    /// <param name="captions">The list of captions.</param>
    /// <param name="startIndex">The start index of overlap.</param>
    /// <param name="endIndex">The end index of overlap.</param>
    /// <returns>The estimated capacity needed.</returns>
    private int EstimateOverlapCapacity(List<ClosedCaption> captions, int startIndex, int endIndex)
    {
        var estimatedLength = 0;
        
        // Calculate actual length of captions in the overlap range
        for (int i = startIndex; i <= endIndex; i++)
        {
            estimatedLength += captions[i].Text.Length + 1; // +1 for space
        }
        
        // Add 25% buffer to account for additional captions that might be added
        return estimatedLength + (estimatedLength / 4);
    }

    /// <summary>
    /// Calculates the optimal StringBuilder capacity based on transcript characteristics.
    /// </summary>
    /// <param name="captions">The list of captions.</param>
    /// <returns>The optimal capacity for StringBuilder initialization.</returns>
    private int CalculateOptimalChunkCapacity(List<ClosedCaption> captions)
    {
        // For very short transcripts, use the total length
        if (captions.Count <= 10)
        {
            var totalLength = captions.Sum(c => c.Text.Length + 1);
            return Math.Max(totalLength, TranscriptConstants.ChunkSizeCharacters / 4);
        }

        // For normal transcripts, use the configured chunk size with buffer
        var baseCapacity = TranscriptConstants.ChunkSizeCharacters;
        
        // Sample first few captions to estimate average caption length
        var sampleSize = Math.Min(5, captions.Count);
        var averageCaptionLength = captions.Take(sampleSize)
            .Average(c => c.Text.Length + 1);
        
        // Estimate how many captions will fit in a chunk
        var estimatedCaptionsPerChunk = baseCapacity / averageCaptionLength;
        
        // Calculate overlap capacity
        var overlapCapacity = Math.Min(
            TranscriptConstants.ChunkOverlapCharacters,
            estimatedCaptionsPerChunk * averageCaptionLength);
        
        // Total capacity = chunk size + overlap + 25% buffer
        var totalCapacity = baseCapacity + overlapCapacity + (baseCapacity / 4);
        
        // Ensure minimum reasonable capacity
        return Math.Max((int)totalCapacity, TranscriptConstants.ChunkSizeCharacters / 2);
    }
}