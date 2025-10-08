// UI Module
// DOM manipulation helpers and UI state management

/**
 * Show an element by removing the 'hidden' class
 * @param {string} id - Element ID
 */
export function showElement(id) {
  const element = document.getElementById(id);
  if (element) {
    element.classList.remove('hidden');
  }
}

/**
 * Hide an element by adding the 'hidden' class
 * @param {string} id - Element ID
 */
export function hideElement(id) {
  const element = document.getElementById(id);
  if (element) {
    element.classList.add('hidden');
  }
}

/**
 * Toggle element visibility
 * @param {string} id - Element ID
 */
export function toggleElement(id) {
  const element = document.getElementById(id);
  if (element) {
    element.classList.toggle('hidden');
  }
}

/**
 * Set the inner HTML of an element
 * @param {string} id - Element ID
 * @param {string} html - HTML content
 */
export function setHTML(id, html) {
  const element = document.getElementById(id);
  if (element) {
    element.innerHTML = html;
  }
}

/**
 * Append HTML to an element
 * @param {string} id - Element ID
 * @param {string} html - HTML content to append
 */
export function appendHTML(id, html) {
  const element = document.getElementById(id);
  if (element) {
    element.insertAdjacentHTML('beforeend', html);
  }
}

/**
 * Clear the content of an element
 * @param {string} id - Element ID
 */
export function clearElement(id) {
  const element = document.getElementById(id);
  if (element) {
    element.innerHTML = '';
  }
}

/**
 * Show an error toast message
 * @param {string} message - Error message to display
 */
export function showError(message) {
  const errorMessageElement = document.getElementById('error-message');
  if (errorMessageElement) {
    errorMessageElement.textContent = message;
  }
  
  showElement('error-toast');
  
  // Auto-hide after 5 seconds
  setTimeout(() => {
    hideElement('error-toast');
  }, 5000);
}

/**
 * Show the add video modal
 */
export function showModal() {
  showElement('add-video-modal');
  // Focus on the URL input
  const urlInput = document.getElementById('video-url');
  if (urlInput) {
    setTimeout(() => urlInput.focus(), 100);
  }
}

/**
 * Hide the add video modal
 */
export function hideModal() {
  hideElement('add-video-modal');
  // Clear the input and error messages
  const urlInput = document.getElementById('video-url');
  const urlError = document.getElementById('url-error');
  const analyzeError = document.getElementById('analyze-error');
  
  if (urlInput) urlInput.value = '';
  if (urlError) {
    urlError.textContent = '';
    urlError.classList.add('hidden');
  }
  if (analyzeError) {
    analyzeError.textContent = '';
    analyzeError.classList.add('hidden');
  }
}

/**
 * Show or hide loading state on the analyze button
 * @param {boolean} isLoading - Whether to show loading state
 */
export function setLoading(isLoading) {
  const analyzeBtn = document.getElementById('analyze-btn');
  const analyzeBtnText = document.getElementById('analyze-btn-text');
  const analyzeSpinner = document.getElementById('analyze-spinner');
  
  if (analyzeBtn) {
    analyzeBtn.disabled = isLoading;
  }
  
  if (analyzeBtnText) {
    analyzeBtnText.textContent = isLoading ? 'Analyzing...' : 'Analyze';
  }
  
  if (analyzeSpinner) {
    if (isLoading) {
      analyzeSpinner.classList.remove('hidden');
    } else {
      analyzeSpinner.classList.add('hidden');
    }
  }
}

/**
 * Show URL validation error
 * @param {string} message - Error message
 */
export function showUrlError(message) {
  const urlError = document.getElementById('url-error');
  if (urlError) {
    urlError.textContent = message;
    urlError.classList.remove('hidden');
  }
}

/**
 * Hide URL validation error
 */
export function hideUrlError() {
  const urlError = document.getElementById('url-error');
  if (urlError) {
    urlError.classList.add('hidden');
  }
}

/**
 * Show analyze error in modal
 * @param {string} message - Error message
 */
export function showAnalyzeError(message) {
  const analyzeError = document.getElementById('analyze-error');
  if (analyzeError) {
    analyzeError.textContent = message;
    analyzeError.classList.remove('hidden');
  }
}

/**
 * Hide analyze error in modal
 */
export function hideAnalyzeError() {
  const analyzeError = document.getElementById('analyze-error');
  if (analyzeError) {
    analyzeError.classList.add('hidden');
  }
}

/**
 * Enable or disable the chat input
 * @param {boolean} enabled - Whether to enable the input
 */
export function setChatInputEnabled(enabled) {
  const questionInput = document.getElementById('question-input');
  const sendBtn = document.getElementById('send-btn');
  
  if (questionInput) {
    questionInput.disabled = !enabled;
  }
  
  if (sendBtn) {
    sendBtn.disabled = !enabled;
  }
}

/**
 * Clear the chat input
 */
export function clearChatInput() {
  const questionInput = document.getElementById('question-input');
  if (questionInput) {
    questionInput.value = '';
  }
}

/**
 * Scroll chat messages to the bottom
 */
export function scrollToChatBottom() {
  const chatMessages = document.getElementById('chat-messages');
  if (chatMessages) {
    setTimeout(() => {
      chatMessages.scrollTop = chatMessages.scrollHeight;
    }, 100);
  }
}

/**
 * Update theme toggle button
 * @param {string} theme - Current theme ('light' or 'dark')
 */
export function updateThemeButton(theme) {
  const themeToggle = document.getElementById('theme-toggle');
  const themeIcon = themeToggle?.querySelector('span');
  
  if (themeIcon) {
    themeIcon.textContent = theme === 'light' ? '☀️' : '🌙';
  }
}

/**
 * Add a streaming cursor to an element
 * @param {string} elementId - ID of the element
 */
export function addStreamingCursor(elementId) {
  const element = document.getElementById(elementId);
  if (element && !element.querySelector('.streaming-cursor')) {
    const cursor = document.createElement('span');
    cursor.className = 'streaming-cursor';
    element.appendChild(cursor);
  }
}

/**
 * Remove the streaming cursor from an element
 * @param {string} elementId - ID of the element
 */
export function removeStreamingCursor(elementId) {
  const element = document.getElementById(elementId);
  if (element) {
    const cursor = element.querySelector('.streaming-cursor');
    if (cursor) {
      cursor.remove();
    }
  }
}

/**
 * Get the value of an input element
 * @param {string} id - Element ID
 * @returns {string} - Input value
 */
export function getInputValue(id) {
  const element = document.getElementById(id);
  return element ? element.value : '';
}

/**
 * Set the value of an input element
 * @param {string} id - Element ID
 * @param {string} value - Value to set
 */
export function setInputValue(id, value) {
  const element = document.getElementById(id);
  if (element) {
    element.value = value;
  }
}

/**
 * Check if an element exists
 * @param {string} id - Element ID
 * @returns {boolean} - True if element exists
 */
export function elementExists(id) {
  return !!document.getElementById(id);
}

/**
 * Add an event listener to an element
 * @param {string} id - Element ID
 * @param {string} event - Event name
 * @param {Function} handler - Event handler function
 */
export function addEventListener(id, event, handler) {
  const element = document.getElementById(id);
  if (element) {
    element.addEventListener(event, handler);
  }
}

/**
 * Remove an event listener from an element
 * @param {string} id - Element ID
 * @param {string} event - Event name
 * @param {Function} handler - Event handler function
 */
export function removeEventListener(id, event, handler) {
  const element = document.getElementById(id);
  if (element) {
    element.removeEventListener(event, handler);
  }
}