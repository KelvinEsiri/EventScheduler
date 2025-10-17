// Service Worker Registration for EventScheduler PWA

if ('serviceWorker' in navigator) {
    window.addEventListener('load', async () => {
        try {
            console.log('[PWA] Registering service worker...');
            const registration = await navigator.serviceWorker.register('/service-worker.js', {
                scope: '/'
            });

            console.log('[PWA] Service worker registered successfully');
            console.log('[PWA] Scope:', registration.scope);

            // Check for updates every hour
            setInterval(() => {
                registration.update();
            }, 60 * 60 * 1000);

            // Listen for service worker updates
            registration.addEventListener('updatefound', () => {
                const newWorker = registration.installing;
                console.log('[PWA] Service worker update found');

                newWorker.addEventListener('statechange', () => {
                    if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                        console.log('[PWA] New service worker installed, reload to update');
                        // Optionally show a notification to the user
                        showUpdateNotification();
                    }
                });
            });

        } catch (error) {
            console.error('[PWA] Service worker registration failed:', error);
        }
    });
}

// Show update notification (optional)
function showUpdateNotification() {
    // This can be integrated with a toast notification system
    console.log('[PWA] A new version is available. Reload to update.');
}

console.log('[PWA] Service worker registration script loaded');
