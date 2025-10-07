namespace InsightStream.Application.DTOs;

public sealed record AskQuestionRequest
{
    public required string VideoId { get; init; }
    public required string Question { get; init; }
}