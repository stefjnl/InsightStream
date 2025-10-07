using Microsoft.AspNetCore.Mvc;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Application.Interfaces.Factories;
using Microsoft.Extensions.AI;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IYouTubeTranscriptService _transcriptService;
    private readonly IChatClientFactory _chatClientFactory;

    public TestController(
        IYouTubeTranscriptService transcriptService,
        IChatClientFactory chatClientFactory)
    {
        _transcriptService = transcriptService;
        _chatClientFactory = chatClientFactory;
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
}