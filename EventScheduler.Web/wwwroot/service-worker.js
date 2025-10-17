const CACHE_VERSION = 'v1';
const CACHE_NAME = `eventscheduler-${CACHE_VERSION}`;

const ASSETS_TO_CACHE = [
    '/',
    '/css/main.css',
    '/css/app.css',
    '/css/auth.css',
    '/css/layout.css',
    '/css/calendar.css',
    '/css/events.css',
    '/css/components/toast-notification.css',
    '/css/components/reconnection-modal.css',
    '/css/components/loading-spinner.css',
    '/css/components/connection-indicator.css',
    '/css/pages/home.css',
    '/css/pages/calendar-view.css',
    '/css/pages/calendar-list.css',
    '/css/pages/public-events.css',
    '/js/offline-storage.js',
    '/js/network-status.js',
    '/js/reconnection-handler.js',
    '/js/fullcalendar-interop.js',
    '/favicon.png',
    'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css',
    'https://cdn.jsdelivr.net/npm/fullcalendar@6.1.10/index.global.min.css',
    'https://cdn.jsdelivr.net/npm/fullcalendar@6.1.10/index.global.min.js'
];

self.addEventListener('install', (event) => {
    console.log('[ServiceWorker] Installing...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => {
                console.log('[ServiceWorker] Caching app shell');
                return cache.addAll(ASSETS_TO_CACHE);
            })
            .catch((error) => {
                console.error('[ServiceWorker] Cache failed:', error);
            })
    );
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    console.log('[ServiceWorker] Activating...');
    event.waitUntil(
        caches.keys().then((cacheNames) => {
            return Promise.all(
                cacheNames.map((cacheName) => {
                    if (cacheName !== CACHE_NAME) {
                        console.log('[ServiceWorker] Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
    return self.clients.claim();
});

self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);

    if (request.method !== 'GET') {
        return;
    }

    if (url.pathname.startsWith('/api/')) {
        event.respondWith(
            fetch(request)
                .catch(() => {
                    return new Response(
                        JSON.stringify({ error: 'Network unavailable' }),
                        {
                            status: 503,
                            headers: { 'Content-Type': 'application/json' }
                        }
                    );
                })
        );
        return;
    }

    event.respondWith(
        caches.match(request)
            .then((cachedResponse) => {
                if (cachedResponse) {
                    return cachedResponse;
                }

                return fetch(request)
                    .then((response) => {
                        if (!response || response.status !== 200 || response.type !== 'basic') {
                            return response;
                        }

                        const responseToCache = response.clone();
                        caches.open(CACHE_NAME)
                            .then((cache) => {
                                cache.put(request, responseToCache);
                            });

                        return response;
                    })
                    .catch((error) => {
                        console.error('[ServiceWorker] Fetch failed:', error);
                        if (request.destination === 'document') {
                            return caches.match('/');
                        }
                        throw error;
                    });
            })
    );
});

self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
