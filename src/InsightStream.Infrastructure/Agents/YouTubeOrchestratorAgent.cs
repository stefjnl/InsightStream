using System.ComponentModel;
using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Factories;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace InsightStream.Infrastructure.Agents;

/// <summary>
/// Orchestrator agent that coordinates video processing workflows.
/// </summary>
public class YouTubeOrchestratorAgent : IYouTubeOrchestrator
{
    private readonly IChatClientFactory _chatClientFactory;
    private readonly IYouTubeTranscriptService _youTubeTranscriptService;
    private readonly IVideoCacheService _videoCacheService;
    private readonly ILogger<YouTubeOrchestratorAgent> _logger;

    /// <summary>
    /// Initializes a new instance of the YouTubeOrchestratorAgent class.
    /// </summary>
    /// <param name="chatClientFactory">The chat client factory.</param>
    /// <param name="youTubeTranscriptService">The YouTube transcript service.</param>
    /// <param name="videoCacheService">The video cache service.</param>
    /// <param name="logger">The logger.</param>
    public YouTubeOrchestratorAgent(
        IChatClientFactory chatClientFactory,
        IYouTubeTranscriptService youTubeTranscriptService,
        IVideoCacheService videoCacheService,
        ILogger<YouTubeOrchestratorAgent> logger)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _youTubeTranscriptService = youTubeTranscriptService ?? throw new ArgumentNullException(nameof(youTubeTranscriptService));
        _videoCacheService = videoCacheService ?? throw new ArgumentNullException(nameof(videoCacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes an analyze request for a YouTube video, extracting content and generating a summary.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated summary text for the video.</returns>
    public async Task<string> ProcessAnalyzeRequestAsync(string videoUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing analyze request for video URL: {VideoUrl}", videoUrl);

            // Create chat client with tools
            var chatClient = CreateChatClientWithTools();

            // Create system message
            var systemMessage = new ChatMessage(
                ChatRole.System,
                @"You are a YouTube video analysis assistant. When a user provides a YouTube video URL and asks for analysis:
1. First, extract the video content using the ExtractVideoContent tool
2. Then, generate a comprehensive summary using the GenerateSummary tool
3. Provide a natural language response summarizing the video content"
            );

            // Create user message
            var userMessage = new ChatMessage(
                ChatRole.User,
                $"Please analyze this YouTube video: {videoUrl}"
            );

            // Send request to LLM with tools
            var response = await chatClient.GetResponseAsync([systemMessage, userMessage], cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully processed analyze request for video URL: {VideoUrl}", videoUrl);
            return response.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing analyze request for video URL: {VideoUrl}", videoUrl);
            throw;
        }
    }

    /// <summary>
    /// Processes a question request about a specific video, maintaining conversation context.
    /// </summary>
    /// <param name="videoId">The ID of the video to question about.</param>
    /// <param name="question">The question to answer about the video content.</param>
    /// <param name="history">The conversation history for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A streaming response with the answer to the question.</returns>
    public async IAsyncEnumerable<string> ProcessQuestionRequestAsync(string videoId, string question, List<ConversationMessage> history, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing question request for video: {VideoId}, Question: {Question}", videoId, question);

        // Create chat client with tools
        var chatClient = CreateChatClientWithTools();

        // Create system message
        var systemMessage = new ChatMessage(
            ChatRole.System,
            @"You are a YouTube video Q&A assistant. When a user asks a question about a video:
1. Use the AnswerQuestion tool to get information from the video content
2. Provide a natural language response answering the user's question based on the video content
3. If the video content hasn't been extracted yet, you may need to use ExtractVideoContent first"
        );

        // Convert conversation history to chat messages
        var chatMessages = new List<ChatMessage> { systemMessage };
        
        if (history != null)
        {
            foreach (var msg in history)
            {
                chatMessages.Add(new ChatMessage(
                    msg.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                    msg.Content
                ));
            }
        }

        // Add current question
        chatMessages.Add(new ChatMessage(
            ChatRole.User,
            question
        ));

        // Send request to LLM with tools and stream response
        await foreach (var chunk in chatClient.GetStreamingResponseAsync(chatMessages, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Text))
            {
                yield return chunk.Text;
            }
        }

        _logger.LogInformation("Successfully processed question request for video: {VideoId}", videoId);
    }

    /// <summary>
    /// Creates a chat client with registered tools.
    /// </summary>
    /// <returns>A chat client with tools registered.</returns>
    private IChatClient CreateChatClientWithTools()
    {
        var chatClient = _chatClientFactory.CreateClient();

        // Register tools with the chat client
        var extractVideoContentFunction = AIFunctionFactory.Create(ExtractVideoContent);
        var generateSummaryFunction = AIFunctionFactory.Create(GenerateSummary);
        var answerQuestionFunction = AIFunctionFactory.Create(AnswerQuestion);

        return chatClient.AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }

    /// <summary>
    /// Tool function to extract video content using YouTubeTranscriptService.
    /// </summary>
    /// <param name="videoUrl">The YouTube video URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A message indicating the result of the extraction.</returns>
    [Description("Extracts video content including metadata and transcript chunks from a YouTube URL")]
    private async Task<string> ExtractVideoContent(string videoUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extracting video content from URL: {VideoUrl}", videoUrl);

            // Extract content using the YouTube transcript service
            var extractionResult = await _youTubeTranscriptService.ExtractTranscriptAsync(videoUrl, cancellationToken);

            // Check if video session already exists
            var existingSession = await _videoCacheService.GetVideoSessionAsync(extractionResult.VideoId, cancellationToken);
            if (existingSession == null)
            {
                // Create new video session
                var newSession = new VideoSession
                {
                    VideoId = extractionResult.VideoId,
                    Metadata = extractionResult.Metadata,
                    Chunks = extractionResult.Chunks,
                    ConversationHistory = new List<ConversationMessage>()
                };

                await _videoCacheService.SetVideoSessionAsync(newSession, cancellationToken);
                _logger.LogInformation("Created new video session for video: {VideoId}", extractionResult.VideoId);
            }
            else
            {
                _logger.LogInformation("Video session already exists for video: {VideoId}", extractionResult.VideoId);
            }

            _logger.LogInformation(
                "Successfully extracted video content for {VideoId}: {Title} with {ChunkCount} chunks",
                extractionResult.VideoId,
                extractionResult.Metadata.Title,
                extractionResult.Chunks.Count);

            return $"Successfully extracted video content for {extractionResult.Metadata.Title} with {extractionResult.Chunks.Count} transcript chunks.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting video content from URL: {VideoUrl}", videoUrl);
            return $"Failed to extract video content: {ex.Message}";
        }
    }

    /// <summary>
    /// Tool function to generate a summary using LLM.
    /// </summary>
    /// <param name="videoId">The ID of the video to summarize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated summary text.</returns>
    [Description("Generates a comprehensive summary for a video that has been previously extracted")]
    private async Task<string> GenerateSummary(string videoId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating summary for video: {VideoId}", videoId);

            // Check if video exists
            var videoExists = await _videoCacheService.VideoExistsAsync(videoId, cancellationToken);
            if (!videoExists)
            {
                _logger.LogWarning("Video not found for ID: {VideoId}", videoId);
                return $"Video not found for ID: {videoId}";
            }

            // Get video session
            var videoSession = await _videoCacheService.GetVideoSessionAsync(videoId, cancellationToken);
            if (videoSession == null)
            {
                _logger.LogWarning("Video session not found for ID: {VideoId}", videoId);
                return $"Video session not found for ID: {videoId}";
            }

            // Create chat client for summarization
            var chatClient = _chatClientFactory.CreateClient();

            // Combine transcript chunks into a single text
            var fullTranscript = string.Join(" ", videoSession.Chunks.Select(c => c.Text));

            // Create messages for summarization
            var messages = new[]
            {
                new ChatMessage(
                    ChatRole.System,
                    "You are a video summarization expert. Create a comprehensive, well-structured summary of the provided video transcript. Include the main topics, key insights, and important details."
                ),
                new ChatMessage(
                    ChatRole.User,
                    $"Please create a summary for this video titled '{videoSession.Metadata.Title}':\n\n{fullTranscript}"
                )
            };

            // Generate summary using LLM
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            var summary = response.Text ?? string.Empty;

            // Update video session with summary
            videoSession.Summary = summary;
            await _videoCacheService.SetVideoSessionAsync(videoSession, cancellationToken);

            _logger.LogInformation("Successfully generated summary for video: {VideoId}", videoId);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for video: {VideoId}", videoId);
            return $"Failed to generate summary: {ex.Message}";
        }
    }

    /// <summary>
    /// Tool function to answer questions using LLM.
    /// </summary>
    /// <param name="videoId">The ID of the video.</param>
    /// <param name="question">The question to answer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The answer to the question.</returns>
    [Description("Answers a question about a video that has been previously extracted")]
    private async Task<string> AnswerQuestion(string videoId, string question, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Answering question for video: {VideoId}, Question: {Question}", videoId, question);

            // Check if video exists
            var videoExists = await _videoCacheService.VideoExistsAsync(videoId, cancellationToken);
            if (!videoExists)
            {
                _logger.LogWarning("Video not found for ID: {VideoId}", videoId);
                return $"Video not found for ID: {videoId}";
            }

            // Get video session
            var videoSession = await _videoCacheService.GetVideoSessionAsync(videoId, cancellationToken);
            if (videoSession == null)
            {
                _logger.LogWarning("Video session not found for ID: {VideoId}", videoId);
                return $"Video session not found for ID: {videoId}";
            }

            // Create chat client for Q&A
            var chatClient = _chatClientFactory.CreateClient();

            // Combine transcript chunks into a single text
            var fullTranscript = string.Join(" ", videoSession.Chunks.Select(c => c.Text));

            // Create messages for Q&A
            var messages = new[]
            {
                new ChatMessage(
                    ChatRole.System,
                    $"You are a video Q&A expert. Answer questions based on the provided video transcript. The video is titled '{videoSession.Metadata.Title}'. Be accurate, concise, and helpful."
                ),
                new ChatMessage(
                    ChatRole.User,
                    $"Based on this video transcript:\n\n{fullTranscript}\n\nAnswer this question: {question}"
                )
            };

            // Generate answer using LLM
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            var answer = response.Text ?? string.Empty;

            _logger.LogInformation("Successfully answered question for video: {VideoId}", videoId);
            return answer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error answering question for video: {VideoId}, Question: {Question}", videoId, question);
            return $"Failed to answer question: {ex.Message}";
        }
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