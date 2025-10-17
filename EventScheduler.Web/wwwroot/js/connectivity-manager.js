// Connectivity Manager for EventScheduler
// Monitors online/offline status and triggers sync

window.connectivityManager = (function () {
    let isOnline = navigator.onLine;
    let dotNetHelper = null;
    let reconnectAttempts = 0;
    const MAX_RECONNECT_ATTEMPTS = 5;
    const RECONNECT_DELAY = 3000; // 3 seconds

    // Initialize connectivity monitoring
    function init(dotNetReference) {
        console.log('[Connectivity] Initializing connectivity manager');
        dotNetHelper = dotNetReference;

        // Set initial state
        isOnline = navigator.onLine;
        console.log('[Connectivity] Initial state:', isOnline ? 'ONLINE' : 'OFFLINE');

        // Listen for online event
        window.addEventListener('online', handleOnline);

        // Listen for offline event
        window.addEventListener('offline', handleOffline);

        // Periodic connectivity check (every 30 seconds)
        setInterval(checkConnectivity, 30000);

        return isOnline;
    }

    // Handle online event
    function handleOnline() {
        console.log('[Connectivity] 🟢 Connection restored');
        isOnline = true;
        reconnectAttempts = 0;

        // Notify Blazor component
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnConnectivityChanged', true);
        }

        // Trigger sync
        triggerSync();
    }

    // Handle offline event
    function handleOffline() {
        console.log('[Connectivity] 🔴 Connection lost');
        isOnline = false;

        // Notify Blazor component
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnConnectivityChanged', false);
        }
    }

    // Check connectivity by pinging the API
    async function checkConnectivity() {
        try {
            const apiBaseUrl = localStorage.getItem('apiBaseUrl') || 'http://localhost:5006';
            const response = await fetch(`${apiBaseUrl}/`, {
                method: 'GET',
                cache: 'no-cache',
                headers: {
                    'Cache-Control': 'no-cache'
                }
            });

            const newOnlineState = response.ok;
            
            if (newOnlineState !== isOnline) {
                console.log('[Connectivity] Status changed:', newOnlineState ? 'ONLINE' : 'OFFLINE');
                isOnline = newOnlineState;

                // Notify Blazor component
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnConnectivityChanged', isOnline);
                }

                if (isOnline) {
                    triggerSync();
                }
            }
        } catch (error) {
            if (isOnline) {
                console.log('[Connectivity] Connection check failed, marking as offline');
                isOnline = false;
                
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnConnectivityChanged', false);
                }
            }
        }
    }

    // Trigger sync when connection is restored
    async function triggerSync() {
        console.log('[Connectivity] Triggering sync...');

        // Request background sync if available
        if ('serviceWorker' in navigator && 'sync' in ServiceWorkerRegistration.prototype) {
            try {
                const registration = await navigator.serviceWorker.ready;
                await registration.sync.register('sync-events');
                console.log('[Connectivity] Background sync registered');
            } catch (error) {
                console.error('[Connectivity] Background sync failed:', error);
                // Fallback to immediate sync
                performImmediateSync();
            }
        } else {
            // Background sync not supported, perform immediate sync
            performImmediateSync();
        }
    }

    // Perform immediate sync (fallback)
    async function performImmediateSync() {
        console.log('[Connectivity] Performing immediate sync...');
        
        if (dotNetHelper) {
            try {
                await dotNetHelper.invokeMethodAsync('TriggerSync');
                console.log('[Connectivity] Immediate sync completed');
            } catch (error) {
                console.error('[Connectivity] Immediate sync failed:', error);
            }
        }
    }

    // Get current connectivity status
    function getStatus() {
        return isOnline;
    }

    // Force connectivity check
    async function forceCheck() {
        await checkConnectivity();
        return isOnline;
    }

    // Cleanup
    function dispose() {
        window.removeEventListener('online', handleOnline);
        window.removeEventListener('offline', handleOffline);
        dotNetHelper = null;
        console.log('[Connectivity] Connectivity manager disposed');
    }

    // Public API
    return {
        init,
        getStatus,
        forceCheck,
        dispose,
        triggerSync
    };
})();

console.log('[Connectivity] Connectivity Manager loaded');
