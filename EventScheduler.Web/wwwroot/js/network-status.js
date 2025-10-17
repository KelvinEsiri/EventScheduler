window.networkStatus = (function() {
    let dotNetRef = null;
    let isServerReachable = true;
    let isBlazorConnected = true; // Track Blazor connection state
    let serverCheckInterval = null;
    let apiBaseUrl = null;
    
    function initialize(dotNetReference, apiUrl) {
        dotNetRef = dotNetReference;
        apiBaseUrl = apiUrl || 'http://localhost:5006';
        
        // Listen for browser online/offline events
        window.addEventListener('online', handleBrowserOnline);
        window.addEventListener('offline', handleBrowserOffline);
        
        // Listen for Blazor connection state
        if (window.Blazor) {
            // Hook into Blazor's reconnection events
            const originalReconnectionHandler = window.Blazor.defaultReconnectionHandler;
            if (originalReconnectionHandler) {
                const originalOnConnectionDown = originalReconnectionHandler.onConnectionDown;
                originalReconnectionHandler.onConnectionDown = function() {
                    console.log('[NetworkStatus] Blazor connection down - treating as offline');
                    isBlazorConnected = false;
                    handleServerUnreachable();
                    if (originalOnConnectionDown) {
                        originalOnConnectionDown.apply(this, arguments);
                    }
                };
                
                const originalOnConnectionUp = originalReconnectionHandler.onConnectionUp;
                originalReconnectionHandler.onConnectionUp = function() {
                    console.log('[NetworkStatus] Blazor connection restored - treating as online');
                    isBlazorConnected = true;
                    handleServerReachable();
                    if (originalOnConnectionUp) {
                        originalOnConnectionUp.apply(this, arguments);
                    }
                };
            }
        }
        
        // Start periodic server health checks
        startServerHealthCheck();
        
        console.log('[NetworkStatus] Monitor initialized - checking both browser and server connectivity');
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
            // Wait a bit for Blazor to reconnect before notifying
            setTimeout(() => notifyOnline(), 1000);
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
            // Only notify if Blazor is actually connected
            if (!isBlazorConnected) {
                console.log('[NetworkStatus] Blazor not reconnected yet, will retry on next health check');
                return; // Skip notification, will retry when Blazor reconnects
            }
            
            // Connection is ready, safe to invoke
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

