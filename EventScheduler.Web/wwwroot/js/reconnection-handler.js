// ═══════════════════════════════════════════════════════════════════════════════
// EventScheduler - Blazor Server Reconnection Handler
// Based on NasosoTax implementation (BLAZOR_RECONNECTION_AND_AUTH_PERSISTENCE.md)
// 
// Purpose: Handles SignalR connection loss and automatic reconnection
// Strategy: Active polling with 2-second intervals until server responds
// Result: Automatic page reload triggers authentication restoration
// ═══════════════════════════════════════════════════════════════════════════════

console.log('🚀 [SignalR] Reconnection handler script loading...');

// Clean up reconnection query parameter if present (from previous offline reconnect)
if (window.location.search.includes('_reconnect=')) {
    const url = new URL(window.location.href);
    url.searchParams.delete('_reconnect');
    // Use replaceState to clean URL without reloading
    window.history.replaceState({}, document.title, url.toString());
    console.log('🧹 [SignalR] Cleaned up reconnection parameter from URL');
}

// Function to wait for Blazor to be ready
function waitForBlazor(callback, maxAttempts = 100, attempt = 1) {
    console.log(`🔍 [SignalR] Checking for Blazor (attempt ${attempt}/${maxAttempts})...`);
    
    const modal = document.getElementById('components-reconnect-modal');
    
    if (!modal) {
        if (attempt < maxAttempts) {
            setTimeout(() => waitForBlazor(callback, maxAttempts, attempt + 1), 100);
        } else {
            console.error('❌ [SignalR] Reconnection modal not found after max attempts!');
        }
        return;
    }
    
    if (window.Blazor && window.Blazor.defaultReconnectionHandler) {
        console.log('✅ [SignalR] Blazor.defaultReconnectionHandler found');
        console.log('✅ [SignalR] Modal found');
        callback(modal);
    } else if (attempt < maxAttempts) {
        setTimeout(() => waitForBlazor(callback, maxAttempts, attempt + 1), 100);
    } else {
        console.warn('⚠️ [SignalR] Blazor.defaultReconnectionHandler not available after max attempts - app may work in degraded mode');
    }
}

// Blazor Server Reconnection UI Handler
waitForBlazor((modal) => {
    
    const handler = window.Blazor.defaultReconnectionHandler;
    const origDown = handler.onConnectionDown;
    const origUp = handler.onConnectionUp;
    let checkServerInterval;
    
    // ═══════════════════════════════════════════════════════════════
    // CONNECTION DOWN HANDLER - COMPLETELY OVERRIDE BLAZOR'S DEFAULT
    // ═══════════════════════════════════════════════════════════════
    handler.onConnectionDown = function() {
        console.log('🔴 [SignalR] Connection lost - enabling offline mode');
        
        // Don't show the blocking modal - app works offline!
        modal.className = 'components-reconnect-hide';
        
        console.log('💡 [SignalR] App continues to work offline - polling silently in background');
        
        // Mark FullCalendar as disconnected so it uses pure JS mode
        if (window.fullCalendarInterop) {
            window.fullCalendarInterop.isBlazorConnected = false;
            console.log('📅 [SignalR] FullCalendar switched to offline mode');
        }
        
        // Check if server is back every 2 seconds
        checkServerInterval = setInterval(async () => {
            console.log('📡 [SignalR] Polling server...');
            try {
                const response = await fetch('/');
                if (response.ok) {
                    console.log('🟢 [SignalR] Server is back! Reloading page to restore connection...');
                    clearInterval(checkServerInterval);
                    // Force a hard reload with timestamp to clear cached state
                    const url = new URL(window.location.href);
                    url.searchParams.set('_reconnect', Date.now().toString());
                    window.location.href = url.toString();
                }
            } catch (e) {
                console.log('❌ [SignalR] Server still down, will retry...');
                // Server still down, keep checking
            }
        }, 2000);
        
        // DON'T call Blazor's original handler - we're handling everything ourselves!
        // This prevents Blazor from showing its reconnection UI or breaking the page
    };
    
    // ═══════════════════════════════════════════════════════════════
    // CONNECTION UP HANDLER - COMPLETELY OVERRIDE BLAZOR'S DEFAULT
    // ═══════════════════════════════════════════════════════════════
    handler.onConnectionUp = function() {
        console.log('🟢 [SignalR] Connection restored - reloading page to restore clean state');
        clearInterval(checkServerInterval);
        
        // Force a hard reload to clear all cached Blazor state
        // This avoids "The list of component operations is not valid" errors
        // Setting location.href with timestamp forces a fresh load
        const url = new URL(window.location.href);
        url.searchParams.set('_reconnect', Date.now().toString());
        window.location.href = url.toString();
        
        // DON'T call Blazor's original handler - we're handling everything ourselves!
    };
    
    console.log('✅ [SignalR] Reconnection handler installed successfully!');
    console.log('🎯 [SignalR] Will poll server every 2 seconds when connection is lost');
});

console.log('✅ [SignalR] Reconnection handler script loaded');
