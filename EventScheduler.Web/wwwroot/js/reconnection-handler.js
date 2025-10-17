// ═══════════════════════════════════════════════════════════════════════════════
// EventScheduler - Blazor Server Reconnection Handler
// Based on NasosoTax implementation (BLAZOR_RECONNECTION_AND_AUTH_PERSISTENCE.md)
// 
// Purpose: Handles SignalR connection loss and automatic reconnection
// Strategy: Active polling with 2-second intervals until server responds
// Result: Automatic page reload triggers authentication restoration
// ═══════════════════════════════════════════════════════════════════════════════

console.log('🚀 [SignalR] Reconnection handler script loading...');

// Blazor Server Reconnection UI Handler
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
    
    // ═══════════════════════════════════════════════════════════════
    // CONNECTION DOWN HANDLER - WRAP BLAZOR'S DEFAULT
    // ═══════════════════════════════════════════════════════════════
    handler.onConnectionDown = function() {
        console.log('🔴 [SignalR] Connection lost - starting active polling');
        modal.className = 'components-reconnect-show';
        
        // Check if server is back every 2 seconds
        checkServerInterval = setInterval(async () => {
            console.log('📡 [SignalR] Polling server...');
            try {
                const response = await fetch('/');
                if (response.ok) {
                    console.log('🟢 [SignalR] Server is back! Reloading page...');
                    clearInterval(checkServerInterval);
                    location.reload();
                }
            } catch (e) {
                console.log('❌ [SignalR] Server still down, will retry...');
                // Server still down, keep checking
            }
        }, 2000);
        
        // Call Blazor's original handler
        if (origDown) origDown.call(handler);
    };
    
    // ═══════════════════════════════════════════════════════════════
    // CONNECTION UP HANDLER - WRAP BLAZOR'S DEFAULT
    // ═══════════════════════════════════════════════════════════════
    handler.onConnectionUp = function() {
        console.log('🟢 [SignalR] Connection restored!');
        clearInterval(checkServerInterval);
        modal.className = 'components-reconnect-hide';
        
        // Call Blazor's original handler
        if (origUp) origUp.call(handler);
    };
    
    console.log('✅ [SignalR] Reconnection handler installed successfully!');
    console.log('🎯 [SignalR] Will poll server every 2 seconds when connection is lost');
}, 1000);

console.log('✅ [SignalR] Reconnection handler script loaded');
