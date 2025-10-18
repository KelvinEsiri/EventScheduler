// EventScheduler Service Worker - Offline Support
// Implements caching strategies for offline functionality

const CACHE_VERSION = 'v2'; // Updated for API URL fix
const CACHE_NAME = `eventscheduler-cache-${CACHE_VERSION}`;

// Resources to cache immediately on install
const STATIC_RESOURCES = [
    '/',
    '/offline.html',
    '/manifest.json',
    '/favicon.png',
    '/app.css',
    '/css/main.css'
];

// Install event - cache static resources
self.addEventListener('install', (event) => {
    console.log('[Service Worker] Installing...');
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            console.log('[Service Worker] Caching static resources');
            return cache.addAll(STATIC_RESOURCES).catch(err => {
                console.error('[Service Worker] Failed to cache some resources:', err);
            });
        })
    );
    // Force the waiting service worker to become the active service worker
    self.skipWaiting();
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
    console.log('[Service Worker] Activating...');
    event.waitUntil(
        caches.keys().then((cacheNames) => {
            return Promise.all(
                cacheNames.map((cacheName) => {
                    if (cacheName !== CACHE_NAME) {
                        console.log('[Service Worker] Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
    // Claim all clients immediately
    return self.clients.claim();
});

// Fetch event - network-first strategy with cache fallback
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);

    // Skip non-GET requests
    if (request.method !== 'GET') {
        return;
    }

    // Skip Blazor SignalR connections
    if (url.pathname.includes('/_blazor') || url.pathname.includes('/hubs/')) {
        return;
    }

    // For API requests, use network-first with cache fallback
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(
            fetch(request)
                .then((response) => {
                    // Clone the response before caching
                    const responseClone = response.clone();
                    caches.open(CACHE_NAME).then((cache) => {
                        cache.put(request, responseClone);
                    });
                    return response;
                })
                .catch(() => {
                    // If network fails, try cache
                    return caches.match(request).then((cachedResponse) => {
                        if (cachedResponse) {
                            return cachedResponse;
                        }
                        // Return offline page if no cache available
                        return caches.match('/offline.html');
                    });
                })
        );
        return;
    }

    // For static resources, use cache-first strategy
    event.respondWith(
        caches.match(request).then((cachedResponse) => {
            if (cachedResponse) {
                return cachedResponse;
            }

            return fetch(request).then((response) => {
                // Don't cache non-successful responses
                if (!response || response.status !== 200) {
                    return response;
                }

                // Clone the response before caching
                const responseClone = response.clone();
                caches.open(CACHE_NAME).then((cache) => {
                    cache.put(request, responseClone);
                });

                return response;
            }).catch(() => {
                // Return offline page if both cache and network fail
                return caches.match('/offline.html');
            });
        })
    );
});

// Background sync event for syncing offline changes
self.addEventListener('sync', (event) => {
    console.log('[Service Worker] Background sync triggered:', event.tag);
    
    if (event.tag === 'sync-events') {
        event.waitUntil(syncOfflineEvents());
    }
});

// Function to sync offline events with the server
async function syncOfflineEvents() {
    console.log('[Service Worker] Syncing offline events...');
    
    try {
        // Open IndexedDB and get pending operations
        const db = await openIndexedDB();
        const pendingOps = await getPendingOperations(db);
        
        if (pendingOps.length === 0) {
            console.log('[Service Worker] No pending operations to sync');
            return;
        }

        console.log(`[Service Worker] Found ${pendingOps.length} pending operations`);
        
        // Process each pending operation
        for (const op of pendingOps) {
            try {
                await processSyncOperation(op);
                await removePendingOperation(db, op.id);
            } catch (error) {
                console.error('[Service Worker] Failed to sync operation:', op, error);
            }
        }
        
        console.log('[Service Worker] Sync completed');
    } catch (error) {
        console.error('[Service Worker] Sync failed:', error);
        throw error; // Rethrow to trigger retry
    }
}

// Helper function to open IndexedDB
function openIndexedDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('EventSchedulerDB', 1);
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

// Helper function to get pending operations from IndexedDB
function getPendingOperations(db) {
    return new Promise((resolve, reject) => {
        const transaction = db.transaction(['pendingOperations'], 'readonly');
        const store = transaction.objectStore('pendingOperations');
        const request = store.getAll();
        request.onsuccess = () => resolve(request.result || []);
        request.onerror = () => reject(request.error);
    });
}

// Helper function to remove a pending operation
function removePendingOperation(db, id) {
    return new Promise((resolve, reject) => {
        const transaction = db.transaction(['pendingOperations'], 'readwrite');
        const store = transaction.objectStore('pendingOperations');
        const request = store.delete(id);
        request.onsuccess = () => resolve();
        request.onerror = () => reject(request.error);
    });
}

// Helper function to process a sync operation
async function processSyncOperation(operation) {
    const { type, endpoint, data } = operation;
    
    // Build full API URL - endpoint already contains /api/events/...
    const apiBaseUrl = 'http://localhost:5006';
    const fullUrl = endpoint.startsWith('http') ? endpoint : `${apiBaseUrl}${endpoint}`;
    
    const options = {
        method: type,
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${operation.token}`
        }
    };
    
    if (data) {
        options.body = JSON.stringify(data);
    }
    
    console.log(`[Service Worker] Syncing ${type} ${fullUrl}`);
    const response = await fetch(fullUrl, options);
    
    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    return response.json();
}

console.log('[Service Worker] Service Worker loaded');
