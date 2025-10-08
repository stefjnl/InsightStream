// API Module
// Handles all API calls to the backend

/**
 * Analyze a YouTube video
 * @param {string} videoUrl - YouTube video URL to analyze
 * @returns {Promise<Object>} - Video analysis response
 */
export async function analyzeVideo(videoUrl) {
  try {
    const response = await fetch(`/api/youtube/analyze`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ videoUrl }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error analyzing video:', error);
    throw error;
  }
}

/**
 * Stream an answer to a question about a video
 * @param {string} videoId - ID of the video
 * @param {string} question - Question to ask
 * @param {Function} onChunk - Callback function for each chunk of data
 * @param {Function} onComplete - Callback function when streaming is complete
 * @param {Function} onError - Callback function for errors
 * @returns {Promise<void>}
 */
export async function streamAnswer(videoId, question, onChunk, onComplete, onError) {
  try {
    const response = await fetch(`/api/youtube/ask`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        videoId,
        question
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    // Get the response body as a stream
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let fullContent = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        
        if (done) {
          break;
        }

        // Decode the chunk and add to buffer
        buffer += decoder.decode(value, { stream: true });
        
        // Process complete SSE messages
        const lines = buffer.split('\n');
        buffer = lines.pop() || ''; // Keep incomplete line in buffer
        
        for (const line of lines) {
          if (line.trim() === '') {
            continue; // Skip empty lines
          }
          
          if (line.startsWith('data: ')) {
            const data = line.substring(6); // Remove 'data: ' prefix
            
            if (data === '[DONE]') {
              // Streaming is complete
              if (onComplete) onComplete(fullContent);
              return;
            }
            
            try {
              // Parse the JSON data
              const parsedData = JSON.parse(data);
              
              // Extract content from the response
              if (parsedData.content) {
                fullContent += parsedData.content;
                
                // Call the chunk callback
                if (onChunk) {
                  onChunk(parsedData.content, fullContent);
                }
              }
            } catch (parseError) {
              console.warn('Failed to parse SSE data:', data, parseError);
              // Try to use the data as-is if it's not valid JSON
              if (data) {
                fullContent += data;
                if (onChunk) {
                  onChunk(data, fullContent);
                }
              }
            }
          }
        }
      }
      
      // Handle any remaining content
      if (buffer.trim() && buffer.startsWith('data: ')) {
        const data = buffer.substring(6);
        if (data !== '[DONE]') {
          fullContent += data;
          if (onChunk) {
            onChunk(data, fullContent);
          }
        }
      }
      
      // Signal completion
      if (onComplete) onComplete(fullContent);
    } finally {
      reader.releaseLock();
    }
  } catch (error) {
    console.error('Error streaming answer:', error);
    if (onError) onError(error);
  }
}

/**
 * Validate YouTube URL
 * @param {string} url - URL to validate
 * @returns {boolean} - True if valid YouTube URL
 */
export function isValidYouTubeUrl(url) {
  if (!url || typeof url !== 'string') {
    return false;
  }
  
  const youtubeRegex = /^(https?:\/\/)?(www\.)?(youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})/;
  return youtubeRegex.test(url);
}

/**
 * Extract video ID from YouTube URL
 * @param {string} url - YouTube URL
 * @returns {string|null} - Video ID or null if not found
 */
export function extractVideoId(url) {
  if (!isValidYouTubeUrl(url)) {
    return null;
  }
  
  const regex = /(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})/;
  const match = url.match(regex);
  return match ? match[1] : null;
}

/**
 * Get YouTube thumbnail URL for a video ID
 * @param {string} videoId - YouTube video ID
 * @param {string} quality - Thumbnail quality: 'default', 'medium', 'high', 'maxres'
 * @returns {string} - Thumbnail URL
 */
export function getThumbnailUrl(videoId, quality = 'medium') {
  const qualityMap = {
    default: 'default',
    medium: 'mqdefault',
    high: 'hqdefault',
    maxres: 'maxresdefault'
  };
  
  const qualityKey = qualityMap[quality] || 'mqdefault';
  return `https://img.youtube.com/vi/${videoId}/${qualityKey}.jpg`;
}