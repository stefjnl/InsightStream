// App Module
// Main application logic, event handlers, and initialization

import state, { mutations } from './state.js';
import { analyzeVideo, streamAnswer, isValidYouTubeUrl, extractVideoId } from './api.js';
import * as ui from './ui.js';
import {
  renderVideoCard,
  renderSummary,
  renderChatMessage,
  renderChatWelcome,
  formatTime
} from './components.js';

/**
 * Initialize the application
 */
function initApp() {
  // Load theme from localStorage
  const savedTheme = mutations.loadTheme();
  ui.updateThemeButton(savedTheme);
  
  // Attach event listeners
  attachEventListeners();
  
  // Initial UI update
  updateUI();
}

/**
 * Attach all event listeners
 */
function attachEventListeners() {
  // Add video button
  ui.addEventListener('add-video-btn', 'click', () => {
    ui.showModal();
  });
  
  // Modal close button
  ui.addEventListener('close-modal-btn', 'click', () => {
    ui.hideModal();
  });
  
  // Modal cancel button
  ui.addEventListener('cancel-btn', 'click', () => {
    ui.hideModal();
  });
  
  // Modal backdrop click
  ui.addEventListener('add-video-modal', 'click', (event) => {
    if (event.target.id === 'add-video-modal') {
      ui.hideModal();
    }
  });
  
  // Analyze button
  ui.addEventListener('analyze-btn', 'click', handleAnalyze);
  
  // Video URL input validation
  ui.addEventListener('video-url', 'input', (event) => {
    const url = event.target.value.trim();
    if (url && !isValidYouTubeUrl(url)) {
      ui.showUrlError('Please enter a valid YouTube URL');
    } else {
      ui.hideUrlError();
    }
  });
  
  // Video URL input enter key
  ui.addEventListener('video-url', 'keypress', (event) => {
    if (event.key === 'Enter') {
      handleAnalyze();
    }
  });
  
  // Chat form submission
  ui.addEventListener('chat-form', 'submit', handleSendMessage);
  
  // Theme toggle
  ui.addEventListener('theme-toggle', 'click', toggleTheme);
  
  // Error toast close button
  ui.addEventListener('close-toast-btn', 'click', () => {
    ui.hideElement('error-toast');
  });
  
  // Keyboard shortcuts
  document.addEventListener('keydown', (event) => {
    // Escape key closes modal
    if (event.key === 'Escape') {
      const modal = document.getElementById('add-video-modal');
      if (!modal.classList.contains('hidden')) {
        ui.hideModal();
      }
    }
    
    // Ctrl/Cmd + K focuses chat input
    if ((event.ctrlKey || event.metaKey) && event.key === 'k') {
      event.preventDefault();
      const chatInput = document.getElementById('question-input');
      if (chatInput && !chatInput.disabled) {
        chatInput.focus();
      }
    }
  });
}

/**
 * Handle video analysis
 */
async function handleAnalyze() {
  const videoUrl = ui.getInputValue('video-url').trim();
  
  // Validate URL
  if (!videoUrl) {
    ui.showUrlError('Please enter a YouTube URL');
    return;
  }
  
  if (!isValidYouTubeUrl(videoUrl)) {
    ui.showUrlError('Please enter a valid YouTube URL');
    return;
  }
  
  ui.hideUrlError();
  ui.hideAnalyzeError();
  ui.setLoading(true);
  
  try {
    // Call the API to analyze the video
    const response = await analyzeVideo(videoUrl);
    
    // Add video to state
    mutations.addVideo(response);
    
    // Update UI
    updateUI();
    
    // Close modal
    ui.hideModal();
    
    // Show success feedback (optional)
    console.log('Video analyzed successfully:', response);
  } catch (error) {
    console.error('Error analyzing video:', error);
    
    // Show error in modal
    const errorMessage = error.message || 'Failed to analyze video. Please try again.';
    ui.showAnalyzeError(errorMessage);
  } finally {
    ui.setLoading(false);
  }
}

/**
 * Handle sending a chat message
 */
async function handleSendMessage(event) {
  if (event) {
    event.preventDefault();
  }
  
  const question = ui.getInputValue('question-input').trim();
  
  if (!question) {
    return;
  }
  
  const selectedVideo = mutations.getSelectedVideo();
  if (!selectedVideo) {
    ui.showError('Please select a video first');
    return;
  }
  
  // Clear input
  ui.clearChatInput();
  
  // Disable input while streaming
  ui.setChatInputEnabled(false);
  
  // Add user message to state and UI
  const userMessage = {
    role: 'user',
    content: question,
    timestamp: new Date()
  };
  
  mutations.addChatMessage(userMessage);
  renderChatMessages();
  
  // Create assistant message placeholder
  const assistantMessageId = Date.now().toString();
  const assistantMessage = {
    role: 'assistant',
    content: '',
    timestamp: new Date(),
    id: assistantMessageId
  };
  
  mutations.addChatMessage(assistantMessage);
  renderChatMessages();
  
  // Set streaming state
  mutations.setStreaming(true);
  
  try {
    // Stream the answer
    await streamAnswer(
      selectedVideo.id,
      question,
      // onChunk callback
      (chunk, fullContent) => {
        // Update the assistant message in state
        mutations.updateLastChatMessage(fullContent);
        
        // Update the UI
        updateStreamingMessage(assistantMessageId, fullContent);
      },
      // onComplete callback
      (fullContent) => {
        // Finalize the message
        mutations.updateLastChatMessage(fullContent);
        updateStreamingMessage(assistantMessageId, fullContent, false);
        
        // Reset streaming state
        mutations.setStreaming(false);
        
        // Re-enable input
        ui.setChatInputEnabled(true);
        
        // Focus back to input
        document.getElementById('question-input').focus();
      },
      // onError callback
      (error) => {
        console.error('Error streaming answer:', error);
        
        // Update the message with error
        const errorMessage = `Sorry, I encountered an error: ${error.message || 'Unknown error'}`;
        mutations.updateLastChatMessage(errorMessage);
        updateStreamingMessage(assistantMessageId, errorMessage, false);
        
        // Show error toast
        ui.showError(errorMessage);
        
        // Reset streaming state
        mutations.setStreaming(false);
        
        // Re-enable input
        ui.setChatInputEnabled(true);
      }
    );
  } catch (error) {
    console.error('Error in streaming:', error);
    
    // Handle any unexpected errors
    const errorMessage = `Sorry, I encountered an error: ${error.message || 'Unknown error'}`;
    mutations.updateLastChatMessage(errorMessage);
    updateStreamingMessage(assistantMessageId, errorMessage, false);
    
    // Show error toast
    ui.showError(errorMessage);
    
    // Reset streaming state
    mutations.setStreaming(false);
    
    // Re-enable input
    ui.setChatInputEnabled(true);
  }
}

/**
 * Update a streaming message in the UI
 * @param {string} messageId - ID of the message to update
 * @param {string} content - New content
 * @param {boolean} isStreaming - Whether the message is still streaming
 */
function updateStreamingMessage(messageId, content, isStreaming = true) {
  // Find the message element
  const messageElements = document.querySelectorAll('.chat-message.assistant');
  const lastMessageElement = messageElements[messageElements.length - 1];
  
  if (lastMessageElement) {
    const contentElement = lastMessageElement.querySelector('.message-content');
    if (contentElement) {
      contentElement.textContent = content;
    }
    
    // Add or remove streaming cursor
    if (isStreaming) {
      ui.addStreamingCursor(contentElement);
    } else {
      ui.removeStreamingCursor(contentElement);
    }
    
    // Scroll to bottom
    ui.scrollToChatBottom();
  }
}

/**
 * Toggle between light and dark themes
 */
function toggleTheme() {
  const newTheme = state.theme === 'light' ? 'dark' : 'light';
  mutations.setTheme(newTheme);
  ui.updateThemeButton(newTheme);
}

/**
 * Update the entire UI based on current state
 */
function updateUI() {
  updateSourcesList();
  updateMainContent();
  attachDynamicEventListeners();
}

/**
 * Update the sources list in the sidebar
 */
function updateSourcesList() {
  const sourcesList = document.getElementById('sources-list');
  const emptySources = document.getElementById('empty-sources');
  
  if (state.videos.length === 0) {
    sourcesList.innerHTML = '';
    ui.showElement('empty-sources');
  } else {
    ui.hideElement('empty-sources');
    
    // Render video cards
    const videosHTML = state.videos.map(video => {
      const isSelected = video.id === state.selectedVideoId;
      return renderVideoCard(video, isSelected);
    }).join('');
    
    ui.setHTML('sources-list', videosHTML);
  }
}

/**
 * Update the main content area
 */
function updateMainContent() {
  const welcomeState = document.getElementById('welcome-state');
  const contentState = document.getElementById('content-state');
  const summaryCard = document.getElementById('summary-card');
  const chatCard = document.getElementById('chat-card');
  
  const selectedVideo = mutations.getSelectedVideo();
  
  if (selectedVideo) {
    // Show content state, hide welcome state
    ui.hideElement('welcome-state');
    ui.showElement('content-state');
    
    // Show summary card
    ui.showElement('summary-card');
    const summaryHTML = renderSummary(selectedVideo);
    ui.setHTML('summary-content', summaryHTML);
    
    // Show chat card and enable input
    ui.showElement('chat-card');
    ui.setChatInputEnabled(!state.streaming);
    
    // Render chat messages
    renderChatMessages();
  } else {
    // Show welcome state, hide content state
    ui.showElement('welcome-state');
    ui.hideElement('content-state');
    
    // Hide summary and chat cards
    ui.hideElement('summary-card');
    ui.hideElement('chat-card');
  }
}

/**
 * Render chat messages
 */
function renderChatMessages() {
  const chatMessages = document.getElementById('chat-messages');
  const messages = mutations.getChatMessages();
  
  if (messages.length === 0) {
    // Show welcome message
    ui.setHTML('chat-messages', renderChatWelcome());
  } else {
    // Render all messages
    const messagesHTML = messages.map((message, index) => {
      const isLastMessage = index === messages.length - 1;
      const isStreaming = isLastMessage && state.streaming && message.role === 'assistant';
      return renderChatMessage(message, isStreaming);
    }).join('');
    
    ui.setHTML('chat-messages', messagesHTML);
  }
  
  // Scroll to bottom
  ui.scrollToChatBottom();
}

/**
 * Attach event listeners to dynamically created elements
 */
function attachDynamicEventListeners() {
  // Video card clicks
  document.querySelectorAll('.video-source-card').forEach(card => {
    card.addEventListener('click', () => {
      const videoId = card.getAttribute('data-video-id');
      if (videoId) {
        mutations.selectVideo(videoId);
        updateUI();
      }
    });
  });
}

// Initialize the app when DOM is loaded
document.addEventListener('DOMContentLoaded', initApp);

// Export for testing purposes
export {
  initApp,
  handleAnalyze,
  handleSendMessage,
  toggleTheme,
  updateUI
};