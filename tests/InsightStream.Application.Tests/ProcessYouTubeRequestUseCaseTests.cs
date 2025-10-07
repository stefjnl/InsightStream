using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using InsightStream.Application.DTOs;
using InsightStream.Application.UseCases;
using InsightStream.Application.Interfaces.Agents;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Domain.Models;
using Moq;

namespace InsightStream.Application.Tests;

public class ProcessYouTubeRequestUseCaseTests
{
    private readonly Mock<IYouTubeOrchestrator> _mockOrchestrator;
    private readonly Mock<IVideoCacheService> _mockCacheService;
    private readonly Mock<ILogger<ProcessYouTubeRequestUseCase>> _mockLogger;
    private readonly ProcessYouTubeRequestUseCase _useCase;

    public ProcessYouTubeRequestUseCaseTests()
    {
        _mockOrchestrator = new Mock<IYouTubeOrchestrator>();
        _mockCacheService = new Mock<IVideoCacheService>();
        _mockLogger = new Mock<ILogger<ProcessYouTubeRequestUseCase>>();
        
        _useCase = new ProcessYouTubeRequestUseCase(
            _mockOrchestrator.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    #region AnalyzeVideoAsync Tests

    [Fact]
    public async Task AnalyzeVideoAsync_WithValidUrlAndNoCache_ShouldReturnVideoResponse()
    {
        // Arrange
        var request = new AnalyzeRequest { VideoUrl = "https://www.youtube.com/watch?v=test123" };
        var cancellationToken = CancellationToken.None;
        
        var expectedMetadata = new VideoMetadata 
        { 
            Title = "Test Video", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(10) 
        };
        
        var expectedChunks = new List<TranscriptChunk>
        {
            new() { Text = "Test transcript", StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromSeconds(5), ChunkIndex = 0 }
        };
        
        var expectedSummary = "This is a test video summary";
        var expectedVideoId = "test123";

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(expectedVideoId, cancellationToken))
            .ReturnsAsync(false);

        _mockOrchestrator
            .Setup(x => x.ExtractVideoContentAsync(request.VideoUrl, cancellationToken))
            .ReturnsAsync((expectedMetadata, expectedChunks));

        _mockOrchestrator
            .Setup(x => x.GenerateSummaryAsync(expectedVideoId, cancellationToken))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _useCase.AnalyzeVideoAsync(request, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedVideoId, result.VideoId);
        Assert.Equal(expectedMetadata, result.Metadata);
        Assert.Equal(expectedSummary, result.Summary);

        _mockCacheService.Verify(x => x.VideoExistsAsync(expectedVideoId, cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.ExtractVideoContentAsync(request.VideoUrl, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.SetVideoSessionAsync(
            It.Is<VideoSession>(s => 
                s.VideoId == expectedVideoId && 
                s.Metadata == expectedMetadata && 
                s.Chunks == expectedChunks), 
            cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.GenerateSummaryAsync(expectedVideoId, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.UpdateSummaryAsync(expectedVideoId, expectedSummary, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task AnalyzeVideoAsync_WithValidUrlAndCachedVideo_ShouldReturnCachedResponse()
    {
        // Arrange
        var request = new AnalyzeRequest { VideoUrl = "https://www.youtube.com/watch?v=cached123" };
        var cancellationToken = CancellationToken.None;
        
        var expectedVideoId = "cached123";
        var expectedMetadata = new VideoMetadata 
        { 
            Title = "Cached Video", 
            Channel = "Cached Channel", 
            Duration = TimeSpan.FromMinutes(5) 
        };
        
        var expectedSummary = "Cached summary";
        
        var cachedSession = new VideoSession
        {
            VideoId = expectedVideoId,
            Metadata = expectedMetadata,
            Chunks = new List<TranscriptChunk>(),
            Summary = expectedSummary,
            ConversationHistory = new List<ConversationMessage>()
        };

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(expectedVideoId, cancellationToken))
            .ReturnsAsync(true);

        _mockCacheService
            .Setup(x => x.GetVideoSessionAsync(expectedVideoId, cancellationToken))
            .ReturnsAsync(cachedSession);

        // Act
        var result = await _useCase.AnalyzeVideoAsync(request, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedVideoId, result.VideoId);
        Assert.Equal(expectedMetadata, result.Metadata);
        Assert.Equal(expectedSummary, result.Summary);

        _mockCacheService.Verify(x => x.VideoExistsAsync(expectedVideoId, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.GetVideoSessionAsync(expectedVideoId, cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.ExtractVideoContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.GenerateSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://www.vimeo.com/123")]
    public async Task AnalyzeVideoAsync_WithInvalidUrl_ShouldThrowInvalidOperationException(string invalidUrl)
    {
        // Arrange
        var request = new AnalyzeRequest { VideoUrl = invalidUrl };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _useCase.AnalyzeVideoAsync(request, cancellationToken));
        
        Assert.Equal("Invalid YouTube URL format", exception.Message);

        // Verify no further processing was attempted
        _mockCacheService.Verify(x => x.VideoExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ExtractVideoContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=invalid")]
    [InlineData("https://youtu.be/invalid")]
    public async Task AnalyzeVideoAsync_WithValidFormatButInvalidUrl_ShouldProceedWithProcessing(string invalidUrl)
    {
        // Arrange - These URLs are considered valid by VideoId.TryParse but may not actually be valid YouTube URLs
        var request = new AnalyzeRequest { VideoUrl = invalidUrl };
        var cancellationToken = CancellationToken.None;
        var expectedVideoId = "invalid";

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(expectedVideoId, cancellationToken))
            .ReturnsAsync(false);

        _mockOrchestrator
            .Setup(x => x.ExtractVideoContentAsync(request.VideoUrl, cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Invalid YouTube video"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _useCase.AnalyzeVideoAsync(request, cancellationToken));
        
        Assert.Equal("Invalid YouTube video", exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ")]
    public async Task AnalyzeVideoAsync_WithMalformedUrl_ShouldThrowUriFormatException(string malformedUrl)
    {
        // Arrange
        var request = new AnalyzeRequest { VideoUrl = malformedUrl };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UriFormatException>(
            () => _useCase.AnalyzeVideoAsync(request, cancellationToken));

        // Verify no further processing was attempted
        _mockCacheService.Verify(x => x.VideoExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ExtractVideoContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeVideoAsync_WhenOrchestratorThrowsException_ShouldLogAndRethrow()
    {
        // Arrange
        var request = new AnalyzeRequest { VideoUrl = "https://www.youtube.com/watch?v=error123" };
        var cancellationToken = CancellationToken.None;
        var expectedVideoId = "error123";
        var expectedException = new InvalidOperationException("Orchestrator error");

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(expectedVideoId, cancellationToken))
            .ReturnsAsync(false);

        _mockOrchestrator
            .Setup(x => x.ExtractVideoContentAsync(request.VideoUrl, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _useCase.AnalyzeVideoAsync(request, cancellationToken));
        
        Assert.Equal(expectedException, actualException);

        // Verify logging
        VerifyLogError(_mockLogger, $"Failed to analyze video for VideoId: {expectedVideoId}");
    }

    [Fact]
    public async Task AnalyzeVideoAsync_WhenCacheServiceThrowsOnVideoExists_ShouldPropagateException()
    {
        // Arrange
        var request = new AnalyzeRequest { VideoUrl = "https://www.youtube.com/watch?v=cacheerror123" };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Cache service error");

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(It.IsAny<string>(), cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _useCase.AnalyzeVideoAsync(request, cancellationToken));
        
        Assert.Equal(expectedException, actualException);
    }

    [Fact]
    public async Task AnalyzeVideoAsync_WhenCacheServiceThrowsOnSetVideoSession_ShouldPropagateException()
    {
        // Arrange
        var request = new AnalyzeRequest { VideoUrl = "https://www.youtube.com/watch?v=seterror123" };
        var cancellationToken = CancellationToken.None;
        var expectedVideoId = "seterror123";
        var expectedException = new InvalidOperationException("Set video session error");

        var metadata = new VideoMetadata 
        { 
            Title = "Test Video", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(10) 
        };
        
        var chunks = new List<TranscriptChunk>();

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(expectedVideoId, cancellationToken))
            .ReturnsAsync(false);

        _mockOrchestrator
            .Setup(x => x.ExtractVideoContentAsync(request.VideoUrl, cancellationToken))
            .ReturnsAsync((metadata, chunks));

        _mockCacheService
            .Setup(x => x.SetVideoSessionAsync(It.IsAny<VideoSession>(), cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _useCase.AnalyzeVideoAsync(request, cancellationToken));
        
        Assert.Equal(expectedException, actualException);
    }

    #endregion

    #region AskQuestionAsync Tests

    [Fact]
    public async Task AskQuestionAsync_WithValidRequest_ShouldStreamResponseAndUpdateHistory()
    {
        // Arrange
        var request = new AskQuestionRequest 
        { 
            VideoId = "test123", 
            Question = "What is this video about?" 
        };
        var cancellationToken = CancellationToken.None;
        
        var streamedResponse = new[] { "This", " video", " is", " about", " testing." };
        var expectedFullResponse = "This video is about testing.";
        
        var userMessage = new ConversationMessage
        {
            Role = "user",
            Content = request.Question,
            Timestamp = DateTimeOffset.UtcNow
        };

        var assistantMessage = new ConversationMessage
        {
            Role = "assistant",
            Content = expectedFullResponse,
            Timestamp = DateTimeOffset.UtcNow
        };

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(request.VideoId, cancellationToken))
            .ReturnsAsync(true);

        _mockCacheService
            .Setup(x => x.AddConversationMessageAsync(request.VideoId, It.Is<ConversationMessage>(m => m.Role == "user" && m.Content == request.Question), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockCacheService
            .Setup(x => x.AddConversationMessageAsync(request.VideoId, It.Is<ConversationMessage>(m => m.Role == "assistant" && m.Content == expectedFullResponse), cancellationToken))
            .Returns(Task.CompletedTask);

        var mockStream = CreateAsyncEnumerable(streamedResponse);
        _mockOrchestrator
            .Setup(x => x.AnswerQuestionAsync(request.VideoId, request.Question, cancellationToken))
            .Returns(mockStream);

        // Act
        var responseChunks = new List<string>();
        await foreach (var chunk in _useCase.AskQuestionAsync(request, cancellationToken))
        {
            responseChunks.Add(chunk);
        }

        // Assert
        Assert.Equal(streamedResponse, responseChunks);
        
        _mockCacheService.Verify(x => x.VideoExistsAsync(request.VideoId, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.AddConversationMessageAsync(
            request.VideoId, 
            It.Is<ConversationMessage>(m => m.Role == "user" && m.Content == request.Question), 
            cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.AnswerQuestionAsync(request.VideoId, request.Question, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.AddConversationMessageAsync(
            request.VideoId, 
            It.Is<ConversationMessage>(m => m.Role == "assistant" && m.Content == expectedFullResponse), 
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task AskQuestionAsync_WhenVideoDoesNotExist_ShouldReturnErrorMessage()
    {
        // Arrange
        var request = new AskQuestionRequest 
        { 
            VideoId = "nonexistent123", 
            Question = "What is this video about?" 
        };
        var cancellationToken = CancellationToken.None;

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(request.VideoId, cancellationToken))
            .ReturnsAsync(false);

        // Act
        var responseChunks = new List<string>();
        await foreach (var chunk in _useCase.AskQuestionAsync(request, cancellationToken))
        {
            responseChunks.Add(chunk);
        }

        // Assert
        Assert.Single(responseChunks);
        Assert.StartsWith("Error:", responseChunks[0]);
        Assert.Contains("Video must be analyzed before asking questions", responseChunks[0]);
        
        _mockCacheService.Verify(x => x.VideoExistsAsync(request.VideoId, cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.AnswerQuestionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AskQuestionAsync_WhenVideoExistsCheckFails_ShouldReturnErrorMessage()
    {
        // Arrange
        var request = new AskQuestionRequest 
        { 
            VideoId = "error123", 
            Question = "What is this video about?" 
        };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Cache service error");

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(request.VideoId, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act
        var responseChunks = new List<string>();
        await foreach (var chunk in _useCase.AskQuestionAsync(request, cancellationToken))
        {
            responseChunks.Add(chunk);
        }

        // Assert
        Assert.Single(responseChunks);
        Assert.StartsWith("Error:", responseChunks[0]);
        Assert.Contains("Failed to check video existence", responseChunks[0]);
        
        _mockCacheService.Verify(x => x.VideoExistsAsync(request.VideoId, cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.AnswerQuestionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AskQuestionAsync_WhenSaveUserQuestionFails_ShouldReturnErrorMessage()
    {
        // Arrange
        var request = new AskQuestionRequest 
        { 
            VideoId = "test123", 
            Question = "What is this video about?" 
        };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Failed to save question");

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(request.VideoId, cancellationToken))
            .ReturnsAsync(true);

        _mockCacheService
            .Setup(x => x.AddConversationMessageAsync(request.VideoId, It.IsAny<ConversationMessage>(), cancellationToken))
            .ThrowsAsync(expectedException);

        // Act
        var responseChunks = new List<string>();
        await foreach (var chunk in _useCase.AskQuestionAsync(request, cancellationToken))
        {
            responseChunks.Add(chunk);
        }

        // Assert
        Assert.Single(responseChunks);
        Assert.StartsWith("Error:", responseChunks[0]);
        Assert.Contains("Failed to save question", responseChunks[0]);
        
        _mockCacheService.Verify(x => x.VideoExistsAsync(request.VideoId, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.AddConversationMessageAsync(
            request.VideoId, 
            It.Is<ConversationMessage>(m => m.Role == "user" && m.Content == request.Question), 
            cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.AnswerQuestionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AskQuestionAsync_WhenOrchestratorThrowsException_ShouldReturnErrorMessage()
    {
        // Arrange
        var request = new AskQuestionRequest 
        { 
            VideoId = "test123", 
            Question = "What is this video about?" 
        };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Orchestrator error");

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(request.VideoId, cancellationToken))
            .ReturnsAsync(true);

        _mockCacheService
            .Setup(x => x.AddConversationMessageAsync(request.VideoId, It.IsAny<ConversationMessage>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockOrchestrator
            .Setup(x => x.AnswerQuestionAsync(request.VideoId, request.Question, cancellationToken))
            .Returns(() => throw expectedException);

        // Act
        var responseChunks = new List<string>();
        await foreach (var chunk in _useCase.AskQuestionAsync(request, cancellationToken))
        {
            responseChunks.Add(chunk);
        }

        // Assert
        Assert.Single(responseChunks);
        Assert.StartsWith("Error:", responseChunks[0]);
        Assert.Contains("Failed to process question", responseChunks[0]);
        
        _mockCacheService.Verify(x => x.VideoExistsAsync(request.VideoId, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.AddConversationMessageAsync(
            request.VideoId, 
            It.Is<ConversationMessage>(m => m.Role == "user" && m.Content == request.Question), 
            cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.AnswerQuestionAsync(request.VideoId, request.Question, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task AskQuestionAsync_WhenSaveAssistantResponseFails_ShouldStillStreamResponse()
    {
        // Arrange
        var request = new AskQuestionRequest 
        { 
            VideoId = "test123", 
            Question = "What is this video about?" 
        };
        var cancellationToken = CancellationToken.None;
        
        var streamedResponse = new[] { "This", " video", " is", " about", " testing." };
        var expectedException = new InvalidOperationException("Failed to save response");

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(request.VideoId, cancellationToken))
            .ReturnsAsync(true);

        _mockCacheService
            .Setup(x => x.AddConversationMessageAsync(request.VideoId, It.IsAny<ConversationMessage>(), cancellationToken))
            .Returns(Task.CompletedTask);

        var mockStream = CreateAsyncEnumerable(streamedResponse);
        _mockOrchestrator
            .Setup(x => x.AnswerQuestionAsync(request.VideoId, request.Question, cancellationToken))
            .Returns(mockStream);

        // Setup the second call (for assistant message) to throw
        _mockCacheService
            .SetupSequence(x => x.AddConversationMessageAsync(request.VideoId, It.IsAny<ConversationMessage>(), cancellationToken))
            .Returns(Task.CompletedTask) // First call for user message
            .ThrowsAsync(expectedException); // Second call for assistant message

        // Act
        var responseChunks = new List<string>();
        await foreach (var chunk in _useCase.AskQuestionAsync(request, cancellationToken))
        {
            responseChunks.Add(chunk);
        }

        // Assert
        Assert.Equal(streamedResponse, responseChunks);
        
        _mockCacheService.Verify(x => x.VideoExistsAsync(request.VideoId, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.AddConversationMessageAsync(
            request.VideoId, 
            It.Is<ConversationMessage>(m => m.Role == "user" && m.Content == request.Question), 
            cancellationToken), Times.Once);
        _mockOrchestrator.Verify(x => x.AnswerQuestionAsync(request.VideoId, request.Question, cancellationToken), Times.Once);
        _mockCacheService.Verify(x => x.AddConversationMessageAsync(
            request.VideoId, 
            It.Is<ConversationMessage>(m => m.Role == "assistant"), 
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task AskQuestionAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var request = new AskQuestionRequest 
        { 
            VideoId = "test123", 
            Question = "What is this video about?" 
        };
        
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        var streamedResponse = new[] { "This", " video", " is", " about", " testing." };

        _mockCacheService
            .Setup(x => x.VideoExistsAsync(request.VideoId, cancellationToken))
            .ReturnsAsync(true);

        _mockCacheService
            .Setup(x => x.AddConversationMessageAsync(request.VideoId, It.IsAny<ConversationMessage>(), cancellationToken))
            .Returns(Task.CompletedTask);

        var mockStream = CreateAsyncEnumerable(streamedResponse);
        _mockOrchestrator
            .Setup(x => x.AnswerQuestionAsync(request.VideoId, request.Question, cancellationToken))
            .Returns(mockStream);

        // Act
        var responseChunks = new List<string>();
        
        // Cancel after receiving first chunk
        await foreach (var chunk in _useCase.AskQuestionAsync(request, cancellationToken))
        {
            responseChunks.Add(chunk);
            cts.Cancel();
            break;
        }

        // Assert
        Assert.Single(responseChunks);
        Assert.Equal("This", responseChunks[0]);
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<string> CreateAsyncEnumerable(IEnumerable<string> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken); // Small delay to simulate real streaming
            yield return item;
        }
    }

    private static void VerifyLogError(Mock<ILogger<ProcessYouTubeRequestUseCase>> logger, string expectedMessage)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}