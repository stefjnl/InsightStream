using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using InsightStream.Application.Interfaces.Services;
using InsightStream.Domain.Models;

namespace InsightStream.Infrastructure.Services;

/// <summary>
/// Service implementation for caching video sessions and related data using IMemoryCache.
/// </summary>
public sealed class VideoCacheService : IVideoCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<VideoCacheService> _logger;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public VideoCacheService(IMemoryCache cache, ILogger<VideoCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        
        // Configure cache options with specified requirements
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
            SlidingExpiration = TimeSpan.FromHours(4),
            Priority = CacheItemPriority.Normal
        };
    }

    /// <inheritdoc />
    public Task<VideoSession?> GetVideoSessionAsync(string videoId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(videoId);
        
        var cacheKey = GetCacheKey(videoId);
        
        if (_cache.TryGetValue(cacheKey, out VideoSession? session))
        {
            _logger.LogDebug("Cache hit for video session: {VideoId}", videoId);
            return Task.FromResult(session);
        }
        
        _logger.LogDebug("Cache miss for video session: {VideoId}", videoId);
        return Task.FromResult<VideoSession?>(null);
    }

    /// <inheritdoc />
    public Task SetVideoSessionAsync(VideoSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        
        var cacheKey = GetCacheKey(session.VideoId);
        _cache.Set(cacheKey, session, _cacheOptions);
        
        _logger.LogInformation("Cached video session: {VideoId}", session.VideoId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> VideoExistsAsync(string videoId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(videoId);
        
        var cacheKey = GetCacheKey(videoId);
        var exists = _cache.TryGetValue(cacheKey, out _);
        
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task UpdateSummaryAsync(string videoId, string summary, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(videoId);
        ArgumentNullException.ThrowIfNull(summary);
        
        var cacheKey = GetCacheKey(videoId);
        
        if (_cache.TryGetValue(cacheKey, out VideoSession? session) && session != null)
        {
            // Create a copy with updated summary to ensure thread safety
            var updatedSession = new VideoSession
            {
                VideoId = session.VideoId,
                Metadata = session.Metadata,
                Chunks = session.Chunks,
                Summary = summary,
                ConversationHistory = new List<ConversationMessage>(session.ConversationHistory)
            };
            
            _cache.Set(cacheKey, updatedSession, _cacheOptions);
            _logger.LogInformation("Updated summary for video session: {VideoId}", videoId);
            return Task.CompletedTask;
        }
        
        _logger.LogWarning("Attempted to update summary for non-existent video session: {VideoId}", videoId);
        throw new InvalidOperationException($"Video session with ID '{videoId}' not found in cache.");
    }

    /// <inheritdoc />
    public Task AddConversationMessageAsync(string videoId, ConversationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(videoId);
        ArgumentNullException.ThrowIfNull(message);
        
        var cacheKey = GetCacheKey(videoId);
        
        if (_cache.TryGetValue(cacheKey, out VideoSession? session) && session != null)
        {
            // Create a new conversation history with the added message
            var updatedConversationHistory = new List<ConversationMessage>(session.ConversationHistory)
            {
                message
            };
            
            // Create a copy with updated conversation history to ensure thread safety
            var updatedSession = new VideoSession
            {
                VideoId = session.VideoId,
                Metadata = session.Metadata,
                Chunks = session.Chunks,
                Summary = session.Summary,
                ConversationHistory = updatedConversationHistory
            };
            
            _cache.Set(cacheKey, updatedSession, _cacheOptions);
            _logger.LogDebug("Added conversation message to video session: {VideoId}", videoId);
            return Task.CompletedTask;
        }
        
        _logger.LogWarning("Attempted to add conversation message to non-existent video session: {VideoId}", videoId);
        throw new InvalidOperationException($"Video session with ID '{videoId}' not found in cache.");
    }

    /// <summary>
    /// Generates the cache key for a video ID.
    /// </summary>
    /// <param name="videoId">The video ID.</param>
    /// <returns>The cache key.</returns>
    private static string GetCacheKey(string videoId) => $"video_session:{videoId}";
}