namespace InsightStream.Domain.Models;

/// <summary>
/// Represents a YouTube video identifier.
/// </summary>
public sealed record VideoId
{
    public required string Value { get; init; }

    /// <summary>
    /// Attempts to parse a YouTube URL to extract the video ID.
    /// </summary>
    /// <param name="videoUrl">The YouTube URL to parse.</param>
    /// <param name="videoId">The parsed video ID if successful.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string videoUrl, out VideoId? videoId)
    {
        videoId = null;
        
        if (string.IsNullOrWhiteSpace(videoUrl))
            return false;

        // Basic YouTube URL parsing logic
        // This is a simplified implementation - in a real scenario, 
        // this would be more robust or use YoutubeExplode in the infrastructure layer
        var uri = new Uri(videoUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var id = query["v"];

        if (!string.IsNullOrEmpty(id))
        {
            videoId = new VideoId { Value = id };
            return true;
        }

        // Handle youtu.be short URLs
        if (uri.Host.Contains("youtu.be") && !string.IsNullOrEmpty(uri.AbsolutePath))
        {
            videoId = new VideoId { Value = uri.AbsolutePath.Trim('/') };
            return true;
        }

        return false;
    }
}