// Components Module
// HTML generator functions for UI components

import { getThumbnailUrl } from './api.js';

/**
 * Format duration in seconds to human-readable format
 * @param {number} seconds - Duration in seconds
 * @returns {string} - Formatted duration (MM:SS or HH:MM:SS)
 */
export function formatDuration(seconds) {
  if (!seconds || seconds < 0) return '00:00';
  
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const remainingSeconds = Math.floor(seconds % 60);
  
  if (hours > 0) {
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
  } else {
    return `${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
  }
}

/**
 * Format a timestamp to a readable time
 * @param {Date|string} timestamp - Timestamp to format
 * @returns {string} - Formatted time
 */
export function formatTime(timestamp) {
  const date = timestamp instanceof Date ? timestamp : new Date(timestamp);
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

/**
 * Render a video source card
 * @param {Object} video - Video object
 * @param {boolean} isSelected - Whether the video is selected
 * @returns {string} - HTML string for the video card
 */
export function renderVideoCard(video, isSelected = false) {
  const selectedClass = isSelected ? 'selected' : '';
  const thumbnailUrl = getThumbnailUrl(video.id, 'medium');
  const duration = formatDuration(video.duration);
  
  return `
    <div class="video-source-card ${selectedClass}" data-video-id="${video.id}">
      <div class="relative">
        <img src="${thumbnailUrl}" alt="${video.title}" class="video-thumbnail" onerror="this.src='https://via.placeholder.com/320x180/374151/ffffff?text=Video+Not+Available'">
        <div class="video-duration">${duration}</div>
      </div>
      <h4 class="font-medium text-sm mb-1 line-clamp-2">${escapeHtml(video.title)}</h4>
      <p class="text-xs text-secondary-text">${escapeHtml(video.channel || 'Unknown Channel')}</p>
    </div>
  `;
}

/**
 * Render video summary
 * @param {Object} video - Video object with summary data
 * @returns {string} - HTML string for the video summary
 */
export function renderSummary(video) {
  if (!video.summary && !video.keyPoints) {
    return `
      <div class="text-center py-8 text-secondary-text">
        <svg class="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
        </svg>
        <p>No summary available yet</p>
      </div>
    `;
  }
  
  let html = '<div class="summary-content">';
  
  // Video title and basic info
  html += `
    <div class="mb-4">
      <h3 class="text-lg font-semibold mb-2">${escapeHtml(video.title)}</h3>
      <p class="text-sm text-secondary-text mb-3">By ${escapeHtml(video.channel || 'Unknown Channel')}</p>
    </div>
  `;
  
  // Summary section
  if (video.summary) {
    html += `
      <div class="summary-section">
        <h4 class="text-md font-semibold mb-2">Summary</h4>
        <p class="text-sm leading-relaxed">${escapeHtml(video.summary)}</p>
      </div>
    `;
  }
  
  // Key points section
  if (video.keyPoints && video.keyPoints.length > 0) {
    html += `
      <div class="summary-section">
        <h4 class="text-md font-semibold mb-2">Key Points</h4>
        <ul class="summary-bullet-points">
          ${video.keyPoints.map(point => `<li>${escapeHtml(point)}</li>`).join('')}
        </ul>
      </div>
    `;
  }
  
  // Video metadata
  html += `
    <div class="video-metadata">
      <div class="metadata-item">
        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <span>${formatDuration(video.duration)}</span>
      </div>
      ${video.viewCount ? `
        <div class="metadata-item">
          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
          </svg>
          <span>${formatNumber(video.viewCount)} views</span>
        </div>
      ` : ''}
      ${video.publishedAt ? `
        <div class="metadata-item">
          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"></path>
          </svg>
          <span>${formatDate(video.publishedAt)}</span>
        </div>
      ` : ''}
    </div>
  `;
  
  html += '</div>';
  return html;
}

/**
 * Render a chat message
 * @param {Object} message - Message object with role, content, and timestamp
 * @param {boolean} isStreaming - Whether the message is currently streaming
 * @returns {string} - HTML string for the chat message
 */
export function renderChatMessage(message, isStreaming = false) {
  const isUser = message.role === 'user';
  const messageClass = isUser ? 'user' : 'assistant';
  const timestamp = formatTime(message.timestamp);
  
  return `
    <div class="chat-message ${messageClass}">
      <div class="chat-bubble">
        <div class="message-content">${escapeHtml(message.content)}</div>
        ${isStreaming ? '<span class="streaming-cursor"></span>' : ''}
        <div class="timestamp">${timestamp}</div>
      </div>
    </div>
  `;
}

/**
 * Render a welcome message for the chat
 * @returns {string} - HTML string for the welcome message
 */
export function renderChatWelcome() {
  return `
    <div class="chat-message assistant">
      <div class="chat-bubble">
        <div class="message-content">
          <p>Hello! I'm here to help you understand this video. Feel free to ask me any questions about the content.</p>
          <p class="mt-2 text-sm opacity-75">For example, you could ask:</p>
          <ul class="mt-2 space-y-1 text-sm">
            <li>• What are the main topics covered?</li>
            <li>• Can you explain the key concepts in more detail?</li>
            <li>• What specific examples were mentioned?</li>
          </ul>
        </div>
        <div class="timestamp">${formatTime(new Date())}</div>
      </div>
    </div>
  `;
}

/**
 * Format a large number to a readable format
 * @param {number} num - Number to format
 * @returns {string} - Formatted number
 */
export function formatNumber(num) {
  if (!num) return '0';
  
  if (num >= 1000000) {
    return (num / 1000000).toFixed(1) + 'M';
  } else if (num >= 1000) {
    return (num / 1000).toFixed(1) + 'K';
  } else {
    return num.toString();
  }
}

/**
 * Format a date to a readable format
 * @param {Date|string} date - Date to format
 * @returns {string} - Formatted date
 */
export function formatDate(date) {
  const d = date instanceof Date ? date : new Date(date);
  return d.toLocaleDateString([], { year: 'numeric', month: 'short', day: 'numeric' });
}

/**
 * Escape HTML to prevent XSS attacks
 * @param {string} text - Text to escape
 * @returns {string} - Escaped text
 */
export function escapeHtml(text) {
  if (!text) return '';
  
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

/**
 * Render a loading spinner
 * @param {string} size - Size of the spinner (sm, md, lg)
 * @returns {string} - HTML string for the loading spinner
 */
export function renderLoadingSpinner(size = 'md') {
  const sizeClasses = {
    sm: 'w-4 h-4',
    md: 'w-6 h-6',
    lg: 'w-8 h-8'
  };
  
  const sizeClass = sizeClasses[size] || sizeClasses.md;
  
  return `
    <div class="flex justify-center items-center py-4">
      <div class="${sizeClass} border-2 border-primary-accent/30 border-t-primary-accent rounded-full animate-spin"></div>
    </div>
  `;
}

/**
 * Render an empty state
 * @param {string} message - Message to display
 * @param {string} icon - Icon name (optional)
 * @returns {string} - HTML string for the empty state
 */
export function renderEmptyState(message, icon = 'document') {
  const icons = {
    document: `<svg class="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
    </svg>`,
    chat: `<svg class="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
    </svg>`,
    video: `<svg class="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
    </svg>`
  };
  
  const iconHtml = icons[icon] || icons.document;
  
  return `
    <div class="text-center py-8 text-secondary-text">
      ${iconHtml}
      <p>${message}</p>
    </div>
  `;
}