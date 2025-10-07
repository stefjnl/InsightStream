using System.Text.Json;
using InsightStream.Application.DTOs;
using InsightStream.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode.Exceptions;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/youtube")]
public sealed class YouTubeController : ControllerBase
{
    private readonly ProcessYouTubeRequestUseCase _useCase;
    private readonly ILogger<YouTubeController> _logger;

    public YouTubeController(
        ProcessYouTubeRequestUseCase useCase,
        ILogger<YouTubeController> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [HttpPost("analyze")]
    [ProducesResponseType<VideoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoResponse>> AnalyzeVideo(
        [FromBody] AnalyzeRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing video analysis request for URL: {VideoUrl}", request.VideoUrl);

        try
        {
            var response = await _useCase.AnalyzeVideoAsync(request, cancellationToken);
            
            _logger.LogInformation("Video analysis completed successfully for VideoId: {VideoId}", response.VideoId);
            
            return Ok(response);
        }
        catch (VideoUnavailableException ex)
        {
            _logger.LogWarning(ex, "Video unavailable for URL: {VideoUrl}", request.VideoUrl);
            
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Video Analysis Failed",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for URL: {VideoUrl}", request.VideoUrl);
            
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Video Analysis Failed",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for URL: {VideoUrl}", request.VideoUrl);
            
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Video Analysis Failed",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }

    [HttpPost("ask")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task AskQuestion(
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing question request for VideoId: {VideoId}, Question: {Question}", 
            request.VideoId, request.Question);

        // Set response headers for SSE
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            await foreach (var chunk in _useCase.AskQuestionAsync(request, cancellationToken).WithCancellation(cancellationToken))
            {
                await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            // Send completion marker
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            _logger.LogInformation("Question processing completed for VideoId: {VideoId}", request.VideoId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for VideoId: {VideoId}", request.VideoId);
            
            var errorMessage = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"data: {errorMessage}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for VideoId: {VideoId}", request.VideoId);
            
            var errorMessage = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"data: {errorMessage}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (VideoUnavailableException ex)
        {
            _logger.LogWarning(ex, "Video unavailable for VideoId: {VideoId}", request.VideoId);
            
            var errorMessage = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"data: {errorMessage}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during question processing for VideoId: {VideoId}", request.VideoId);
            
            var errorMessage = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"data: {errorMessage}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}