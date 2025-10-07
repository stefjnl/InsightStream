namespace InsightStream.Domain.Models;

public sealed record TranscriptChunk
{
    public required string Text { get; init; }
    public required TimeSpan StartTime { get; init; }
    public required TimeSpan EndTime { get; init; }
    public required int ChunkIndex { get; init; }
}