using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Factories;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace InsightStream.Infrastructure.Agents;

/// <summary>
/// Agent responsible for answering questions about video content.
/// </summary>
public class QuestionAnsweringAgent : IQuestionAnsweringAgent
{
    private readonly IVideoCacheService _videoCacheService;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<QuestionAnsweringAgent> _logger;

    /// <summary>
    /// Initializes a new instance of the QuestionAnsweringAgent class.
    /// </summary>
    /// <param name="videoCacheService">The video cache service.</param>
    /// <param name="chatClientFactory">The chat client factory.</param>
    /// <param name="logger">The logger.</param>
    public QuestionAnsweringAgent(
        IVideoCacheService videoCacheService,
        IChatClientFactory chatClientFactory,
        ILogger<QuestionAnsweringAgent> logger)
    {
        _videoCacheService = videoCacheService ?? throw new ArgumentNullException(nameof(videoCacheService));
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Answers a question about the specified video content.
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

        var videoSession = await _videoCacheService.GetVideoSessionAsync(videoId, cancellationToken);
        if (videoSession == null)
        {
            _logger.LogWarning("Video session not found for video ID: {VideoId}", videoId);
            throw new InvalidOperationException($"Video session not found for video ID: {videoId}");
        }

        // Combine transcript chunks for context
        var fullTranscript = string.Join(" ", videoSession.Chunks.Select(c => c.Text));
        
        // Create chat client
        var chatClient = _chatClientFactory.CreateClient();
        
        // Build conversation history context
        var conversationHistory = videoSession.ConversationHistory
            .TakeLast(10) // Limit to last 10 messages to avoid context overflow
            .Select(msg => $"{msg.Role}: {msg.Content}")
            .ToList();

        var historyContext = conversationHistory.Any()
            ? $"Previous conversation:\n{string.Join("\n", conversationHistory)}\n\n"
            : string.Empty;

        var prompt = $"""
            You are an AI assistant helping answer questions about a YouTube video.
            
            Video Information:
            Title: "{videoSession.Metadata.Title}"
            Channel: "{videoSession.Metadata.Channel}"
            Duration: {videoSession.Metadata.Duration}
            
            {(!string.IsNullOrEmpty(videoSession.Summary) ? $"Video Summary: {videoSession.Summary}\n\n" : "")}
            {historyContext}Video Transcript:
            {fullTranscript}
            
            User Question: {question}
            
            Please provide a helpful and accurate answer based on the video content. If the information is not available in the transcript, please indicate that clearly.
            """;

        // Store the user question in conversation history
        var userMessage = new ConversationMessage
        {
            Role = "user",
            Content = question,
            Timestamp = DateTimeOffset.UtcNow
        };
        await _videoCacheService.AddConversationMessageAsync(videoId, userMessage, cancellationToken);

        // Stream the response
        var fullResponse = new System.Text.StringBuilder();
        
        var messages = new[] { new ChatMessage(ChatRole.User, prompt) };
        await foreach (var chunk in chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            var text = chunk.Text ?? string.Empty;
            fullResponse.Append(text);
            yield return text;
        }

        // Store the assistant response in conversation history
        var assistantMessage = new ConversationMessage
        {
            Role = "assistant",
            Content = fullResponse.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };
        await _videoCacheService.AddConversationMessageAsync(videoId, assistantMessage, cancellationToken);

        _logger.LogInformation("Successfully answered question for video: {VideoId}", videoId);
    }
}