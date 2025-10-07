using InsightStream.Domain.Models;

namespace InsightStream.Application.Interfaces.Services;

/// <summary>
/// Service interface for caching video sessions and related data.
/// </summary>
public interface IVideoCacheService
{
    /// <summary>
    /// Retrieves a video session by its ID.
    /// </summary>
    /// <param name="videoId">The ID of the video to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The video session if found; otherwise, null.</returns>
    Task<VideoSession?> GetVideoSessionAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a video session in the cache.
    /// </summary>
    /// <param name="session">The video session to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetVideoSessionAsync(VideoSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a video exists in the cache.
    /// </summary>
    /// <param name="videoId">The ID of the video to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the video exists; otherwise, false.</returns>
    Task<bool> VideoExistsAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the summary for a video session.
    /// </summary>
    /// <param name="videoId">The ID of the video to update.</param>
    /// <param name="summary">The new summary content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateSummaryAsync(string videoId, string summary, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a conversation message to a video session.
    /// </summary>
    /// <param name="videoId">The ID of the video session.</param>
    /// <param name="message">The conversation message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddConversationMessageAsync(string videoId, ConversationMessage message, CancellationToken cancellationToken = default);
}