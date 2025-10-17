// Network Status Detection for EventScheduler
// Monitors online/offline status and notifies the Blazor application

window.networkStatus = (function() {
    let dotNetRef = null;
    
    function initialize(dotNetReference) {
        dotNetRef = dotNetReference;
        
        window.addEventListener('online', handleOnline);
        window.addEventListener('offline', handleOffline);
        
        console.log('Network status monitor initialized');
    }
    
    function handleOnline() {
        console.log('Network status: Online');
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('UpdateNetworkStatus', true);
        }
    }
    
    function handleOffline() {
        console.log('Network status: Offline');
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('UpdateNetworkStatus', false);
        }
    }
    
    function isOnline() {
        return navigator.onLine;
    }
    
    function cleanup() {
        window.removeEventListener('online', handleOnline);
        window.removeEventListener('offline', handleOffline);
        dotNetRef = null;
        console.log('Network status monitor cleaned up');
    }
    
    return {
        initialize,
        isOnline,
        cleanup
    };
})();
