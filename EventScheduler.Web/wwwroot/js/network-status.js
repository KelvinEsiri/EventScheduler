window.networkStatus = (function() {
    let dotNetRef = null;
    let isServerReachable = true;
    let serverCheckInterval = null;
    let apiBaseUrl = null;
    
    function initialize(dotNetReference, apiUrl) {
        dotNetRef = dotNetReference;
        apiBaseUrl = apiUrl || 'http://localhost:5006';
        
        // Listen for browser online/offline events
        window.addEventListener('online', handleBrowserOnline);
        window.addEventListener('offline', handleBrowserOffline);
        
        // Start periodic server health checks
        startServerHealthCheck();
        
        console.log('[NetworkStatus] Monitor initialized - checking browser and server connectivity');
    }
    
    function handleBrowserOnline() {
        console.log('[NetworkStatus] Browser online event detected');
        checkServerHealth();
    }
    
    function handleBrowserOffline() {
        console.log('[NetworkStatus] Browser offline event detected');
        notifyOffline();
    }
    
    function handleServerReachable() {
        if (!isServerReachable) {
            console.log('[NetworkStatus] Server is now reachable');
            isServerReachable = true;
            notifyOnline();
        }
    }
    
    function handleServerUnreachable() {
        if (isServerReachable) {
            console.log('[NetworkStatus] Server is unreachable');
            isServerReachable = false;
            notifyOffline();
        }
    }
    
    function notifyOnline() {
        console.log('[NetworkStatus] Status: ONLINE');
        if (dotNetRef) {
            try {
                dotNetRef.invokeMethodAsync('UpdateNetworkStatus', true)
                    .catch(err => {
                        console.log('[NetworkStatus] Could not notify online status:', err.message);
                    });
            } catch (err) {
                console.log('[NetworkStatus] Exception notifying online status:', err.message);
            }
        }
    }
    
    function notifyOffline() {
        console.log('[NetworkStatus] Status: OFFLINE');
        if (dotNetRef) {
            // Offline notifications are less critical, try but don't fail
            try {
                dotNetRef.invokeMethodAsync('UpdateNetworkStatus', false)
                    .catch(err => {
                        // Expected when connection is down, ignore
                    });
            } catch (err) {
                // Expected when connection is down, ignore
            }
        }
    }
    
    function startServerHealthCheck() {
        // Check server health every 5 seconds
        serverCheckInterval = setInterval(() => {
            if (navigator.onLine) {
                checkServerHealth();
            } else {
                // Browser is offline, no point checking server
                handleServerUnreachable();
            }
        }, 5000);
    }
    
    async function checkServerHealth() {
        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 3000); // 3 second timeout
            
            const response = await fetch(`${apiBaseUrl}/health`, {
                method: 'GET',
                signal: controller.signal,
                cache: 'no-cache'
            });
            
            clearTimeout(timeoutId);
            
            if (response.ok) {
                handleServerReachable();
            } else {
                handleServerUnreachable();
            }
        } catch (error) {
            // Server unreachable (timeout, network error, CORS, etc.)
            console.log('[NetworkStatus] Server health check failed:', error.message);
            handleServerUnreachable();
        }
    }
    
    function isOnline() {
        // Return true only if BOTH browser is online AND server is reachable
        return navigator.onLine && isServerReachable;
    }
    
    function cleanup() {
        window.removeEventListener('online', handleBrowserOnline);
        window.removeEventListener('offline', handleBrowserOffline);
        
        if (serverCheckInterval) {
            clearInterval(serverCheckInterval);
            serverCheckInterval = null;
        }
        
        dotNetRef = null;
        console.log('[NetworkStatus] Monitor cleaned up');
    }
    
    return {
        initialize,
        isOnline,
        checkServerHealth,
        cleanup
    };
})();

