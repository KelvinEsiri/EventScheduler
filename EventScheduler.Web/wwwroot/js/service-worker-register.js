window.serviceWorkerHelper = {
    register: async function() {
        if ('serviceWorker' in navigator) {
            try {
                const registration = await navigator.serviceWorker.register('/service-worker.js', {
                    scope: '/'
                });
                
                console.log('[ServiceWorker] Registered successfully:', registration.scope);

                registration.addEventListener('updatefound', () => {
                    const newWorker = registration.installing;
                    console.log('[ServiceWorker] Update found, installing new version');
                    
                    newWorker.addEventListener('statechange', () => {
                        if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                            console.log('[ServiceWorker] New version available, please refresh');
                            newWorker.postMessage({ type: 'SKIP_WAITING' });
                        }
                    });
                });

                let refreshing = false;
                navigator.serviceWorker.addEventListener('controllerchange', () => {
                    if (!refreshing) {
                        refreshing = true;
                        console.log('[ServiceWorker] Controller changed, reloading page');
                    }
                });

                return true;
            } catch (error) {
                console.error('[ServiceWorker] Registration failed:', error);
                return false;
            }
        } else {
            console.warn('[ServiceWorker] Not supported in this browser');
            return false;
        }
    },

    unregister: async function() {
        if ('serviceWorker' in navigator) {
            const registrations = await navigator.serviceWorker.getRegistrations();
            for (const registration of registrations) {
                await registration.unregister();
                console.log('[ServiceWorker] Unregistered');
            }
        }
    }
};
