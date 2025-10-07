using InsightStream.Application.DTOs;
using InsightStream.Domain.Models;

namespace InsightStream.Application.Tests;

public class DtoValidationTests
{
    #region AnalyzeRequest Tests

    [Fact]
    public void AnalyzeRequest_WithValidVideoUrl_ShouldCreateInstance()
    {
        // Arrange
        var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var request = new AnalyzeRequest { VideoUrl = videoUrl };

        // Assert
        Assert.NotNull(request);
        Assert.Equal(videoUrl, request.VideoUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void AnalyzeRequest_WithNullOrEmptyVideoUrl_ShouldCreateInstanceButBeInvalid(string videoUrl)
    {
        // Act
        var request = new AnalyzeRequest { VideoUrl = videoUrl };

        // Assert
        Assert.NotNull(request);
        Assert.Equal(videoUrl, request.VideoUrl);
        // Note: Since these are records with no validation attributes,
        // the instance is created but the URL is invalid for business logic
    }

    [Fact]
    public void AnalyzeRequest_ShouldBeImmutableRecord()
    {
        // Arrange
        var originalUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        var request = new AnalyzeRequest { VideoUrl = originalUrl };

        // Act & Assert - Records are immutable, so we can't change properties
        // This test verifies the record behavior by checking equality
        var sameRequest = new AnalyzeRequest { VideoUrl = originalUrl };
        Assert.Equal(request, sameRequest);

        var differentRequest = new AnalyzeRequest { VideoUrl = "https://www.youtube.com/watch?v=abc123" };
        Assert.NotEqual(request, differentRequest);
    }

    [Fact]
    public void AnalyzeRecord_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        var request1 = new AnalyzeRequest { VideoUrl = videoUrl };
        var request2 = new AnalyzeRequest { VideoUrl = videoUrl };

        // Act & Assert
        Assert.Equal(request1, request2);
        Assert.Equal(request1.GetHashCode(), request2.GetHashCode());
    }

    #endregion

    #region AskQuestionRequest Tests

    [Fact]
    public void AskQuestionRequest_WithValidData_ShouldCreateInstance()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var question = "What is this video about?";

        // Act
        var request = new AskQuestionRequest { VideoId = videoId, Question = question };

        // Assert
        Assert.NotNull(request);
        Assert.Equal(videoId, request.VideoId);
        Assert.Equal(question, request.Question);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void AskQuestionRequest_WithNullOrEmptyVideoId_ShouldCreateInstanceButBeInvalid(string videoId)
    {
        // Arrange
        var question = "What is this video about?";

        // Act
        var request = new AskQuestionRequest { VideoId = videoId, Question = question };

        // Assert
        Assert.NotNull(request);
        Assert.Equal(videoId, request.VideoId);
        Assert.Equal(question, request.Question);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void AskQuestionRequest_WithNullOrEmptyQuestion_ShouldCreateInstanceButBeInvalid(string question)
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";

        // Act
        var request = new AskQuestionRequest { VideoId = videoId, Question = question };

        // Assert
        Assert.NotNull(request);
        Assert.Equal(videoId, request.VideoId);
        Assert.Equal(question, request.Question);
    }

    [Fact]
    public void AskQuestionRequest_WithLongQuestion_ShouldCreateInstance()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var longQuestion = new string('a', 1000); // Very long question

        // Act
        var request = new AskQuestionRequest { VideoId = videoId, Question = longQuestion };

        // Assert
        Assert.NotNull(request);
        Assert.Equal(videoId, request.VideoId);
        Assert.Equal(longQuestion, request.Question);
    }

    [Fact]
    public void AskQuestionRequest_WithSpecialCharacters_ShouldCreateInstance()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var question = "¿Qué es este video? 🎥 What's this about? #video @youtube";

        // Act
        var request = new AskQuestionRequest { VideoId = videoId, Question = question };

        // Assert
        Assert.NotNull(request);
        Assert.Equal(videoId, request.VideoId);
        Assert.Equal(question, request.Question);
    }

    [Fact]
    public void AskQuestionRequest_ShouldBeImmutableRecord()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var question = "What is this video about?";
        var request = new AskQuestionRequest { VideoId = videoId, Question = question };

        // Act & Assert - Records are immutable
        var sameRequest = new AskQuestionRequest { VideoId = videoId, Question = question };
        Assert.Equal(request, sameRequest);

        var differentRequest = new AskQuestionRequest { VideoId = "abc123", Question = question };
        Assert.NotEqual(request, differentRequest);

        var differentQuestionRequest = new AskQuestionRequest { VideoId = videoId, Question = "Different question" };
        Assert.NotEqual(request, differentQuestionRequest);
    }

    [Fact]
    public void AskQuestionRequest_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var question = "What is this video about?";
        var request1 = new AskQuestionRequest { VideoId = videoId, Question = question };
        var request2 = new AskQuestionRequest { VideoId = videoId, Question = question };

        // Act & Assert
        Assert.Equal(request1, request2);
        Assert.Equal(request1.GetHashCode(), request2.GetHashCode());
    }

    #endregion

    #region VideoResponse Tests

    [Fact]
    public void VideoResponse_WithValidData_ShouldCreateInstance()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var metadata = new VideoMetadata 
        { 
            Title = "Test Video", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(5) 
        };
        var summary = "This is a test video summary";

        // Act
        var response = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata, 
            Summary = summary 
        };

        // Assert
        Assert.NotNull(response);
        Assert.Equal(videoId, response.VideoId);
        Assert.Equal(metadata, response.Metadata);
        Assert.Equal(summary, response.Summary);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void VideoResponse_WithNullOrEmptyVideoId_ShouldCreateInstance(string videoId)
    {
        // Arrange
        var metadata = new VideoMetadata 
        { 
            Title = "Test Video", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(5) 
        };
        var summary = "This is a test video summary";

        // Act
        var response = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata, 
            Summary = summary 
        };

        // Assert
        Assert.NotNull(response);
        Assert.Equal(videoId, response.VideoId);
        Assert.Equal(metadata, response.Metadata);
        Assert.Equal(summary, response.Summary);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void VideoResponse_WithNullOrEmptySummary_ShouldCreateInstance(string summary)
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var metadata = new VideoMetadata 
        { 
            Title = "Test Video", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(5) 
        };

        // Act
        var response = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata, 
            Summary = summary 
        };

        // Assert
        Assert.NotNull(response);
        Assert.Equal(videoId, response.VideoId);
        Assert.Equal(metadata, response.Metadata);
        Assert.Equal(summary, response.Summary);
    }

    [Fact]
    public void VideoResponse_WithComplexMetadata_ShouldCreateInstance()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var metadata = new VideoMetadata 
        { 
            Title = "Very Long Video Title With Special Characters: 🎥 Test Video #123", 
            Channel = "Channel With Special Characters & Numbers 123", 
            Duration = TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(45)
        };
        var summary = new string('a', 1000); // Very long summary

        // Act
        var response = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata, 
            Summary = summary 
        };

        // Assert
        Assert.NotNull(response);
        Assert.Equal(videoId, response.VideoId);
        Assert.Equal(metadata, response.Metadata);
        Assert.Equal(summary, response.Summary);
    }

    [Fact]
    public void VideoResponse_ShouldBeImmutableRecord()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var metadata = new VideoMetadata 
        { 
            Title = "Test Video", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(5) 
        };
        var summary = "This is a test video summary";
        var response = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata, 
            Summary = summary 
        };

        // Act & Assert - Records are immutable
        var sameResponse = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata, 
            Summary = summary 
        };
        Assert.Equal(response, sameResponse);

        var differentResponse = new VideoResponse 
        { 
            VideoId = "abc123", 
            Metadata = metadata, 
            Summary = summary 
        };
        Assert.NotEqual(response, differentResponse);
    }

    [Fact]
    public void VideoResponse_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var metadata = new VideoMetadata 
        { 
            Title = "Test Video", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(5) 
        };
        var summary = "This is a test video summary";
        
        var response1 = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata, 
            Summary = summary 
        };
        var response2 = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata, 
            Summary = summary 
        };

        // Act & Assert
        Assert.Equal(response1, response2);
        Assert.Equal(response1.GetHashCode(), response2.GetHashCode());
    }

    [Fact]
    public void VideoResponse_WithDifferentMetadata_ShouldNotBeEqual()
    {
        // Arrange
        var videoId = "dQw4w9WgXcQ";
        var summary = "This is a test video summary";
        
        var metadata1 = new VideoMetadata 
        { 
            Title = "Test Video 1", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(5) 
        };
        
        var metadata2 = new VideoMetadata 
        { 
            Title = "Test Video 2", 
            Channel = "Test Channel", 
            Duration = TimeSpan.FromMinutes(5) 
        };
        
        var response1 = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata1, 
            Summary = summary 
        };
        var response2 = new VideoResponse 
        { 
            VideoId = videoId, 
            Metadata = metadata2, 
            Summary = summary 
        };

        // Act & Assert
        Assert.NotEqual(response1, response2);
    }

    #endregion

    #region AnswerResponse Tests

    [Fact]
    public void AnswerResponse_WithValidData_ShouldCreateInstance()
    {
        // Arrange
        var answer = "This is the answer to your question.";
        var chunksUsed = new List<int> { 1, 2, 3, 5 };

        // Act
        var answerResponse = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };

        // Assert
        Assert.NotNull(answerResponse);
        Assert.Equal(answer, answerResponse.Answer);
        Assert.Equal(chunksUsed, answerResponse.ChunksUsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void AnswerResponse_WithNullOrEmptyAnswer_ShouldCreateInstance(string answer)
    {
        // Arrange
        var chunksUsed = new List<int> { 1, 2, 3 };

        // Act
        var answerResponse = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };

        // Assert
        Assert.NotNull(answerResponse);
        Assert.Equal(answer, answerResponse.Answer);
        Assert.Equal(chunksUsed, answerResponse.ChunksUsed);
    }

    [Fact]
    public void AnswerResponse_WithEmptyChunksUsed_ShouldCreateInstance()
    {
        // Arrange
        var answer = "This is the answer.";
        var chunksUsed = new List<int>();

        // Act
        var answerResponse = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };

        // Assert
        Assert.NotNull(answerResponse);
        Assert.Equal(answer, answerResponse.Answer);
        Assert.Empty(answerResponse.ChunksUsed);
    }

    [Fact]
    public void AnswerResponse_WithNullChunksUsed_ShouldCreateInstance()
    {
        // Arrange
        var answer = "This is the answer.";
        List<int> chunksUsed = null!;

        // Act
        var answerResponse = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };

        // Assert
        Assert.NotNull(answerResponse);
        Assert.Equal(answer, answerResponse.Answer);
        Assert.Equal(chunksUsed, answerResponse.ChunksUsed);
    }

    [Fact]
    public void AnswerResponse_WithLongAnswer_ShouldCreateInstance()
    {
        // Arrange
        var longAnswer = new string('a', 10000); // Very long answer
        var chunksUsed = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var answerResponse = new AnswerResponse { Answer = longAnswer, ChunksUsed = chunksUsed };

        // Assert
        Assert.NotNull(answerResponse);
        Assert.Equal(longAnswer, answerResponse.Answer);
        Assert.Equal(chunksUsed, answerResponse.ChunksUsed);
    }

    [Fact]
    public void AnswerResponse_WithSpecialCharacters_ShouldCreateInstance()
    {
        // Arrange
        var answer = "This answer contains special characters: 🎥 ñáéíóú 中文 العربية русский";
        var chunksUsed = new List<int> { 0, 1, 2 };

        // Act
        var answerResponse = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };

        // Assert
        Assert.NotNull(answerResponse);
        Assert.Equal(answer, answerResponse.Answer);
        Assert.Equal(chunksUsed, answerResponse.ChunksUsed);
    }

    [Fact]
    public void AnswerResponse_WithManyChunksUsed_ShouldCreateInstance()
    {
        // Arrange
        var answer = "This answer uses many chunks.";
        var chunksUsed = Enumerable.Range(0, 1000).ToList(); // 1000 chunks

        // Act
        var answerResponse = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };

        // Assert
        Assert.NotNull(answerResponse);
        Assert.Equal(answer, answerResponse.Answer);
        Assert.Equal(1000, answerResponse.ChunksUsed.Count);
        Assert.Equal(0, answerResponse.ChunksUsed.First());
        Assert.Equal(999, answerResponse.ChunksUsed.Last());
    }

    [Fact]
    public void AnswerResponse_ShouldBeImmutableRecord()
    {
        // Arrange
        var answer = "This is the answer to your question.";
        var chunksUsed = new List<int> { 1, 2, 3 };
        var answerResponse1 = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };
        var answerResponse2 = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };

        // Act & Assert
        Assert.Equal(answerResponse1, answerResponse2);
        Assert.Equal(answerResponse1.GetHashCode(), answerResponse2.GetHashCode());

        var differentAnswer = new AnswerResponse { Answer = "Different answer", ChunksUsed = chunksUsed };
        Assert.NotEqual(answerResponse1, differentAnswer);

        var differentChunks = new AnswerResponse { Answer = answer, ChunksUsed = new List<int> { 4, 5, 6 } };
        Assert.NotEqual(answerResponse1, differentChunks);
    }

    [Fact]
    public void AnswerResponse_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var answer = "This is the answer to your question.";
        var chunksUsed = new List<int> { 1, 2, 3 };
        
        var response1 = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };
        var response2 = new AnswerResponse { Answer = answer, ChunksUsed = chunksUsed };

        // Act & Assert
        Assert.Equal(response1, response2);
        Assert.Equal(response1.GetHashCode(), response2.GetHashCode());
    }

    #endregion

    #region Cross-DTO Integration Tests

    [Fact]
    public void DTOs_ShouldWorkTogetherInWorkflow()
    {
        // Arrange - Simulate a complete workflow
        var analyzeRequest = new AnalyzeRequest 
        { 
            VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ" 
        };

        var videoResponse = new VideoResponse 
        { 
            VideoId = "dQw4w9WgXcQ",
            Metadata = new VideoMetadata 
            { 
                Title = "Test Video", 
                Channel = "Test Channel", 
                Duration = TimeSpan.FromMinutes(5) 
            },
            Summary = "This is a test video summary"
        };

        var askQuestionRequest = new AskQuestionRequest 
        { 
            VideoId = videoResponse.VideoId, 
            Question = "What is this video about?" 
        };

        var answerResponse = new AnswerResponse
        {
            Answer = "This video is about testing and validation.",
            ChunksUsed = new List<int> { 0, 1, 2 }
        };

        // Act & Assert - Verify the workflow can be constructed
        Assert.NotNull(analyzeRequest);
        Assert.NotNull(videoResponse);
        Assert.Contains(askQuestionRequest.VideoId, analyzeRequest.VideoUrl);
        Assert.NotNull(askQuestionRequest);
        Assert.NotNull(answerResponse);
    }

    #endregion
}