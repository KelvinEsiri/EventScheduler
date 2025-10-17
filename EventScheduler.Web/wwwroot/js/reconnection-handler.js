console.log('[SignalR] Reconnection handler loading...');

setTimeout(() => {
    const modal = document.getElementById('components-reconnect-modal');
    
    if (!modal) {
        console.error('[SignalR] Reconnection modal not found');
        return;
    }
    
    if (!window.Blazor || !window.Blazor.defaultReconnectionHandler) {
        console.error('[SignalR] Blazor.defaultReconnectionHandler not available');
        return;
    }
    
    console.log('[SignalR] Initializing reconnection handler');
    
    const handler = window.Blazor.defaultReconnectionHandler;
    const origDown = handler.onConnectionDown;
    const origUp = handler.onConnectionUp;
    let checkServerInterval;
    
    handler.onConnectionDown = function() {
        console.log('[SignalR] Connection lost');
        
        const isOffline = !navigator.onLine;
        
        if (isOffline) {
            console.log('[SignalR] User is offline - skipping reconnection UI');
            if (origDown) origDown.call(handler);
            return;
        }
        
        console.log('[SignalR] Starting server polling');
        modal.className = 'components-reconnect-show';
        
        checkServerInterval = setInterval(async () => {
            if (!navigator.onLine) {
                console.log('[SignalR] User went offline - stopping reconnection');
                clearInterval(checkServerInterval);
                modal.className = 'components-reconnect-hide';
                return;
            }
            
            try {
                const response = await fetch('/');
                if (response.ok) {
                    console.log('[SignalR] Server reachable - reconnecting');
                    clearInterval(checkServerInterval);
                    
                    if (window.offlineStorage && window.offlineStorage.getPendingOperationsCount) {
                        const pendingCount = await window.offlineStorage.getPendingOperationsCount();
                        if (pendingCount > 0) {
                            console.log(`[SignalR] ${pendingCount} pending operations will sync after reconnection`);
                        }
                    }
                    
                    modal.className = 'components-reconnect-hide';
                }
            } catch (e) {
                // Server still unreachable, will retry
            }
        }, 2000);
        
        if (origDown) origDown.call(handler);
    };
    
    handler.onConnectionUp = function() {
        console.log('[SignalR] Connection restored');
        clearInterval(checkServerInterval);
        modal.className = 'components-reconnect-hide';
        
        if (origUp) origUp.call(handler);
    };
    
    console.log('[SignalR] Reconnection handler initialized');
}, 1000);
