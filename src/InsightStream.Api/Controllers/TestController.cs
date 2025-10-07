using Microsoft.AspNetCore.Mvc;
using InsightStream.Application.Interfaces.Services;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IYouTubeTranscriptService _transcriptService;

    public TestController(IYouTubeTranscriptService transcriptService)
    {
        _transcriptService = transcriptService;
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
}