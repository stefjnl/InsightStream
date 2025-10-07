namespace InsightStream.Domain.Models;

public sealed class VideoSession
{
    public required string VideoId { get; init; }
    public required VideoMetadata Metadata { get; init; }
    public required IReadOnlyList<TranscriptChunk> Chunks { get; init; }
    public string? Summary { get; set; }
    public List<ConversationMessage> ConversationHistory { get; init; } = new();
}

public sealed record ConversationMessage
{
    public required string Role { get; init; } // "user" or "assistant"
    public required string Content { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}