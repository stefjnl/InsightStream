// State Management Module
// Central state object for the application

const state = {
  videos: [],
  selectedVideoId: null,
  theme: 'dark',
  streaming: false
};

// Mutation functions to update state
export const mutations = {
  /**
   * Add a new video to the state and set it as selected
   * @param {Object} video - Video object to add
   */
  addVideo(video) {
    state.videos.push(video);
    state.selectedVideoId = video.id;
  },

  /**
   * Select a video by ID
   * @param {string} videoId - ID of the video to select
   */
  selectVideo(videoId) {
    if (state.videos.find(v => v.id === videoId)) {
      state.selectedVideoId = videoId;
    }
  },

  /**
   * Get the currently selected video object
   * @returns {Object|null} - Selected video or null if none selected
   */
  getSelectedVideo() {
    return state.videos.find(v => v.id === state.selectedVideoId) || null;
  },

  /**
   * Set the theme and update DOM
   * @param {string} theme - Theme name ('light' or 'dark')
   */
  setTheme(theme) {
    state.theme = theme;
    document.documentElement.className = `theme-${theme}`;
    localStorage.setItem('insightstream-theme', theme);
  },

  /**
   * Load theme from localStorage or use default
   */
  loadTheme() {
    const savedTheme = localStorage.getItem('insightstream-theme') || 'dark';
    state.theme = savedTheme;
    document.documentElement.className = `theme-${savedTheme}`;
    return savedTheme;
  },

  /**
   * Set streaming state
   * @param {boolean} isStreaming - Whether streaming is active
   */
  setStreaming(isStreaming) {
    state.streaming = isStreaming;
  },

  /**
   * Add a chat message to the selected video
   * @param {Object} message - Message object with role, content, and timestamp
   */
  addChatMessage(message) {
    const selectedVideo = this.getSelectedVideo();
    if (selectedVideo) {
      if (!selectedVideo.chatMessages) {
        selectedVideo.chatMessages = [];
      }
      selectedVideo.chatMessages.push(message);
    }
  },

  /**
   * Update the last chat message (used for streaming)
   * @param {string} content - New content for the last message
   */
  updateLastChatMessage(content) {
    const selectedVideo = this.getSelectedVideo();
    if (selectedVideo && selectedVideo.chatMessages && selectedVideo.chatMessages.length > 0) {
      const lastMessage = selectedVideo.chatMessages[selectedVideo.chatMessages.length - 1];
      if (lastMessage.role === 'assistant') {
        lastMessage.content = content;
      }
    }
  },

  /**
   * Get all chat messages for the selected video
   * @returns {Array} - Array of chat messages
   */
  getChatMessages() {
    const selectedVideo = this.getSelectedVideo();
    return selectedVideo ? (selectedVideo.chatMessages || []) : [];
  },

  /**
   * Clear all state (for testing or reset)
   */
  clearState() {
    state.videos = [];
    state.selectedVideoId = null;
    state.streaming = false;
  }
};

// Export the state object directly for read access
export default state;