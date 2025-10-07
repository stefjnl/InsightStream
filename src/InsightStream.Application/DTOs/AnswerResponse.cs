namespace InsightStream.Application.DTOs;

public sealed record AnswerResponse
{
    public required string Answer { get; init; }
    public required IReadOnlyList<int> ChunksUsed { get; init; }
}