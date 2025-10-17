// EventScheduler - Blazor Server Reconnection Handler
// Handles SignalR connection loss and automatic reconnection

console.log('🚀 [SignalR] Reconnection handler script loading...');

setTimeout(() => {
    console.log('🔍 [SignalR] Checking for modal and Blazor...');
    
    const modal = document.getElementById('components-reconnect-modal');
    
    if (!modal) {
        console.error('❌ [SignalR] Reconnection modal not found!');
        return;
    }
    
    console.log('✅ [SignalR] Modal found');
    
    if (!window.Blazor || !window.Blazor.defaultReconnectionHandler) {
        console.error('❌ [SignalR] Blazor.defaultReconnectionHandler not available!');
        return;
    }
    
    console.log('✅ [SignalR] Blazor.defaultReconnectionHandler found');
    
    const handler = window.Blazor.defaultReconnectionHandler;
    const origDown = handler.onConnectionDown;
    const origUp = handler.onConnectionUp;
    let checkServerInterval;
    
    handler.onConnectionDown = function() {
        console.log('🔴 [SignalR] Connection lost - checking if offline mode active');
        
        // Check if we're in offline mode (no network connectivity)
        const isOffline = !navigator.onLine;
        
        if (isOffline) {
            console.log('📴 [SignalR] User is offline - skipping reconnection UI');
            // Don't show reconnection modal when user is intentionally offline
            // The app should continue to work in offline mode
            if (origDown) origDown.call(handler);
            return;
        }
        
        console.log('🔴 [SignalR] Connection lost (online) - starting active polling');
        modal.className = 'components-reconnect-show';
        
        checkServerInterval = setInterval(async () => {
            console.log('📡 [SignalR] Polling server...');
            
            // Check if user went offline while trying to reconnect
            if (!navigator.onLine) {
                console.log('📴 [SignalR] User went offline - stopping reconnection attempts');
                clearInterval(checkServerInterval);
                modal.className = 'components-reconnect-hide';
                return;
            }
            
            try {
                const response = await fetch('/');
                if (response.ok) {
                    console.log('🟢 [SignalR] Server is back! Reloading page...');
                    clearInterval(checkServerInterval);
                    location.reload();
                }
            } catch (e) {
                console.log('❌ [SignalR] Server still down, will retry...');
            }
        }, 2000);
        
        if (origDown) origDown.call(handler);
    };
    
    handler.onConnectionUp = function() {
        console.log('🟢 [SignalR] Connection restored!');
        clearInterval(checkServerInterval);
        modal.className = 'components-reconnect-hide';
        
        if (origUp) origUp.call(handler);
    };
    
    console.log('✅ [SignalR] Reconnection handler installed successfully!');
    console.log('🎯 [SignalR] Will poll server every 2 seconds when connection is lost');
}, 1000);

console.log('✅ [SignalR] Reconnection handler script loaded');
