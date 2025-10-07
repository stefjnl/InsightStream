namespace InsightStream.Domain.Constants;

public static class TranscriptConstants
{
    public const int ChunkSizeTokens = 2000;
    public const int ChunkOverlapTokens = 200;
    
    // Approximate: 1 token ≈ 4 characters (conservative estimate)
    public const int ApproximateCharactersPerToken = 4;
    
    public static int ChunkSizeCharacters => ChunkSizeTokens * ApproximateCharactersPerToken;
    public static int ChunkOverlapCharacters => ChunkOverlapTokens * ApproximateCharactersPerToken;
}