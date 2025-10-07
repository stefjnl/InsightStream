using Microsoft.AspNetCore.Mvc;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Application.Interfaces.Factories;
using InsightStream.Domain.Models;
using Microsoft.Extensions.AI;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IYouTubeTranscriptService _transcriptService;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly IVideoCacheService _cacheService;

    public TestController(
        IYouTubeTranscriptService transcriptService,
        IChatClientFactory chatClientFactory,
        IVideoCacheService cacheService)
    {
        _transcriptService = transcriptService;
        _chatClientFactory = chatClientFactory;
        _cacheService = cacheService;
    }

    [HttpGet("extract")]
    public async Task<IActionResult> TestExtraction([FromQuery] string url)
    {
        try
        {
            var result = await _transcriptService.ExtractTranscriptAsync(url);
            return Ok(new 
            { 
                result.VideoId, 
                result.Metadata,
                ChunkCount = result.Chunks.Count,
                FirstChunk = result.Chunks.FirstOrDefault()?.Text.Length > 100 
                    ? result.Chunks.FirstOrDefault()?.Text[..100] 
                    : result.Chunks.FirstOrDefault()?.Text
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("chat")]
    public async Task<IActionResult> TestChatClient()
    {
        try
        {
            var chatClient = _chatClientFactory.CreateClient();
            
            var messages = new[] { new ChatMessage(ChatRole.User, "Say 'Hello from InsightStream!' in a friendly way.") };
            var response = await chatClient.GetResponseAsync(messages);
            
            return Ok(new
            {
                Model = response.ModelId,
                Message = response.Text,
                FinishReason = response.FinishReason
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("cache")]
    public async Task<IActionResult> TestCache([FromQuery] string url)
    {
        try
        {
            // Extract transcript
            var result = await _transcriptService.ExtractTranscriptAsync(url);
            
            // Create and cache session
            var session = new VideoSession
            {
                VideoId = result.VideoId,
                Metadata = result.Metadata,
                Chunks = result.Chunks,
                Summary = null,
                ConversationHistory = new List<ConversationMessage>()
            };
            
            await _cacheService.SetVideoSessionAsync(session);
            
            // Retrieve from cache
            var cachedSession = await _cacheService.GetVideoSessionAsync(result.VideoId);
            
            // Update summary
            await _cacheService.UpdateSummaryAsync(result.VideoId, "Test summary");
            
            // Add conversation message
            await _cacheService.AddConversationMessageAsync(result.VideoId, new ConversationMessage
            {
                Role = "user",
                Content = "Test question",
                Timestamp = DateTimeOffset.UtcNow
            });
            
            // Retrieve updated session
            var updatedSession = await _cacheService.GetVideoSessionAsync(result.VideoId);
            
            return Ok(new
            {
                VideoId = result.VideoId,
                CachedSuccessfully = cachedSession is not null,
                SummaryUpdated = updatedSession?.Summary == "Test summary",
                ConversationCount = updatedSession?.ConversationHistory.Count,
                ChunkCount = updatedSession?.Chunks.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}