using System.Runtime.CompilerServices;
using System.Linq;
using InsightStream.Application.DTOs;
using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Domain.Models;
using Microsoft.Extensions.Logging;

namespace InsightStream.Application.UseCases;

/// <summary>
/// Use case for processing YouTube video requests including analysis and question answering.
/// </summary>
public sealed class ProcessYouTubeRequestUseCase
{
    private readonly IYouTubeOrchestrator _orchestrator;
    private readonly IVideoCacheService _cacheService;
    private readonly ILogger<ProcessYouTubeRequestUseCase> _logger;

    public ProcessYouTubeRequestUseCase(
        IYouTubeOrchestrator orchestrator,
        IVideoCacheService cacheService,
        ILogger<ProcessYouTubeRequestUseCase> logger)
    {
        _orchestrator = orchestrator;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a YouTube video by extracting content and generating a summary.
    /// </summary>
    /// <param name="request">The analysis request containing the video URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A video response containing metadata and summary.</returns>
    public async Task<VideoResponse> AnalyzeVideoAsync(AnalyzeRequest request, CancellationToken cancellationToken)
    {
        if (!VideoId.TryParse(request.VideoUrl, out var videoId))
        {
            _logger.LogError("Invalid YouTube URL format: {VideoUrl}", request.VideoUrl);
            throw new InvalidOperationException("Invalid YouTube URL format");
        }

        var videoIdString = videoId!.Value;
        _logger.LogInformation("Starting video analysis for VideoId: {VideoId}", videoIdString);

        try
        {
            // Check if video already cached
            if (await _cacheService.VideoExistsAsync(videoIdString, cancellationToken))
            {
                _logger.LogInformation("Video found in cache for VideoId: {VideoId}", videoIdString);
                
                var cachedSession = await _cacheService.GetVideoSessionAsync(videoIdString, cancellationToken);
                if (cachedSession is not null)
                {
                    return new VideoResponse
                    {
                        VideoId = cachedSession.VideoId,
                        Metadata = cachedSession.Metadata,
                        Summary = cachedSession.Summary ?? string.Empty
                    };
                }
            }

            _logger.LogInformation("Extracting video content for VideoId: {VideoId}", videoIdString);
            
            // Extract content from YouTube
            var (metadata, chunks) = await _orchestrator.ExtractVideoContentAsync(request.VideoUrl, cancellationToken);
            
            // Create new video session
            var session = new VideoSession
            {
                VideoId = videoIdString,
                Metadata = metadata,
                Chunks = chunks,
                ConversationHistory = new List<ConversationMessage>()
            };

            // Cache the session
            await _cacheService.SetVideoSessionAsync(session, cancellationToken);
            _logger.LogInformation("Video session cached for VideoId: {VideoId}", videoIdString);

            // Generate summary
            _logger.LogInformation("Generating summary for VideoId: {VideoId}", videoIdString);
            var summary = await _orchestrator.GenerateSummaryAsync(videoIdString, cancellationToken);
            
            // Update cached session with summary
            await _cacheService.UpdateSummaryAsync(videoIdString, summary, cancellationToken);
            session.Summary = summary;

            _logger.LogInformation("Video analysis completed for VideoId: {VideoId}", videoIdString);

            return new VideoResponse
            {
                VideoId = videoIdString,
                Metadata = metadata,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze video for VideoId: {VideoId}", videoIdString);
            throw;
        }
    }

    /// <summary>
    /// Answers a question about a previously analyzed video.
    /// </summary>
    /// <param name="request">The question request containing video ID and question.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A streaming response with the answer.</returns>
    public async IAsyncEnumerable<string> AskQuestionAsync(
        AskQuestionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing question for VideoId: {VideoId}", request.VideoId);

        // Validate cached session exists before streaming
        bool videoExists;
        Exception? validationException = null;
        try
        {
            videoExists = await _cacheService.VideoExistsAsync(request.VideoId, cancellationToken);
        }
        catch (Exception ex)
        {
            validationException = ex;
            videoExists = false;
        }

        if (validationException != null)
        {
            _logger.LogError(validationException, "Failed to check if video exists for VideoId: {VideoId}", request.VideoId);
            yield return $"Error: Failed to check video existence - {validationException.Message}";
            yield break;
        }

        if (!videoExists)
        {
            var errorMessage = $"Video must be analyzed before asking questions. VideoId: {request.VideoId}";
            _logger.LogWarning(errorMessage);
            yield return $"Error: {errorMessage}";
            yield break;
        }

        // Add user question to conversation history
        var userMessage = new ConversationMessage
        {
            Role = "user",
            Content = request.Question,
            Timestamp = DateTimeOffset.UtcNow
        };

        Exception? saveQuestionException = null;
        try
        {
            await _cacheService.AddConversationMessageAsync(request.VideoId, userMessage, cancellationToken);
            _logger.LogDebug("Added user question to conversation history for VideoId: {VideoId}", request.VideoId);
        }
        catch (Exception ex)
        {
            saveQuestionException = ex;
        }

        if (saveQuestionException != null)
        {
            _logger.LogError(saveQuestionException, "Failed to add user question to conversation history for VideoId: {VideoId}", request.VideoId);
            yield return $"Error: Failed to save question - {saveQuestionException.Message}";
            yield break;
        }

        // Get the streaming response from orchestrator
        IAsyncEnumerable<string> responseStream;
        Exception? streamException = null;
        try
        {
            responseStream = _orchestrator.AnswerQuestionAsync(request.VideoId, request.Question, cancellationToken);
        }
        catch (Exception ex)
        {
            streamException = ex;
            responseStream = EmptyAsyncEnumerable();
        }

        // Helper method to create empty async enumerable
        async IAsyncEnumerable<string> EmptyAsyncEnumerable()
        {
            await Task.CompletedTask;
            yield break;
        }

        if (streamException != null)
        {
            _logger.LogError(streamException, "Failed to start answer stream for VideoId: {VideoId}", request.VideoId);
            yield return $"Error: Failed to process question - {streamException.Message}";
            yield break;
        }

        // Stream the response once, collecting it while yielding to the caller
        var responseBuilder = new System.Text.StringBuilder();
        
        await foreach (var chunk in responseStream.WithCancellation(cancellationToken))
        {
            // Collect the chunk for conversation history
            responseBuilder.Append(chunk);
            
            // Stream the chunk to the caller
            yield return chunk;
        }

        var fullResponse = responseBuilder.ToString();

        // Add assistant response to conversation history
        Exception? saveResponseException = null;
        try
        {
            var assistantMessage = new ConversationMessage
            {
                Role = "assistant",
                Content = fullResponse,
                Timestamp = DateTimeOffset.UtcNow
            };

            await _cacheService.AddConversationMessageAsync(request.VideoId, assistantMessage, cancellationToken);
            _logger.LogInformation("Completed question processing for VideoId: {VideoId}", request.VideoId);
        }
        catch (Exception ex)
        {
            saveResponseException = ex;
        }

        if (saveResponseException != null)
        {
            _logger.LogError(saveResponseException, "Failed to save conversation history for VideoId: {VideoId}", request.VideoId);
            // Don't yield here since the response was already streamed
        }
    }
}