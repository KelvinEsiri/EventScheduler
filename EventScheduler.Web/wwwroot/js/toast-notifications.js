// Simple Toast Notification Helper
// Provides lightweight notifications without server calls

window.showToast = function(title, message, type = 'info') {
    // Check if toast container exists, create if not
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = `
            position: fixed;
            bottom: 20px;
            right: 20px;
            z-index: 9999;
            display: flex;
            flex-direction: column;
            gap: 10px;
        `;
        document.body.appendChild(container);
    }
    
    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    
    // Set colors based on type
    const colors = {
        info: { bg: '#3788d8', icon: 'ℹ️' },
        success: { bg: '#28a745', icon: '✅' },
        warning: { bg: '#ffc107', icon: '⚠️' },
        error: { bg: '#dc3545', icon: '❌' }
    };
    
    const style = colors[type] || colors.info;
    
    toast.style.cssText = `
        background: ${style.bg};
        color: white;
        padding: 12px 20px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        min-width: 280px;
        max-width: 400px;
        animation: slideIn 0.3s ease-out;
        display: flex;
        align-items: center;
        gap: 12px;
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    `;
    
    toast.innerHTML = `
        <span style="font-size: 20px;">${style.icon}</span>
        <div style="flex: 1;">
            <div style="font-weight: 600; margin-bottom: 2px;">${title}</div>
            <div style="font-size: 13px; opacity: 0.9;">${message}</div>
        </div>
        <button onclick="this.parentElement.remove()" 
                style="background: rgba(255,255,255,0.2); border: none; color: white; 
                       border-radius: 4px; padding: 4px 8px; cursor: pointer; font-size: 18px;">
            ×
        </button>
    `;
    
    container.appendChild(toast);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease-in';
        setTimeout(() => toast.remove(), 300);
    }, 5000);
};

// Add CSS animations
if (!document.getElementById('toast-styles')) {
    const style = document.createElement('style');
    style.id = 'toast-styles';
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(400px);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(400px);
                opacity: 0;
            }
        }
        
        .toast-notification:hover {
            transform: translateX(-5px);
            transition: transform 0.2s;
        }
    `;
    document.head.appendChild(style);
}

console.log('[Toast] Notification helper loaded');
