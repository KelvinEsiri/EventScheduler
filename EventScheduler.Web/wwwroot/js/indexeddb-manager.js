// IndexedDB Manager for EventScheduler
// Provides client-side persistent storage for offline support

window.indexedDBManager = (function () {
    const DB_NAME = 'EventSchedulerDB';
    const DB_VERSION = 1;
    let db = null;

    // Store names
    const STORES = {
        EVENTS: 'events',
        PENDING_OPERATIONS: 'pendingOperations',
        SYNC_METADATA: 'syncMetadata'
    };

    // Initialize the database
    async function initDB() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(DB_NAME, DB_VERSION);

            request.onerror = () => {
                console.error('[IndexedDB] Error opening database:', request.error);
                reject(request.error);
            };

            request.onsuccess = () => {
                db = request.result;
                console.log('[IndexedDB] Database opened successfully');
                resolve(db);
            };

            request.onupgradeneeded = (event) => {
                console.log('[IndexedDB] Upgrading database...');
                const db = event.target.result;

                // Create events store
                if (!db.objectStoreNames.contains(STORES.EVENTS)) {
                    const eventsStore = db.createObjectStore(STORES.EVENTS, { keyPath: 'id' });
                    eventsStore.createIndex('userId', 'userId', { unique: false });
                    eventsStore.createIndex('startDate', 'startDate', { unique: false });
                    eventsStore.createIndex('syncStatus', 'syncStatus', { unique: false });
                    console.log('[IndexedDB] Created events store');
                }

                // Create pending operations store
                if (!db.objectStoreNames.contains(STORES.PENDING_OPERATIONS)) {
                    const opsStore = db.createObjectStore(STORES.PENDING_OPERATIONS, { 
                        keyPath: 'id', 
                        autoIncrement: true 
                    });
                    opsStore.createIndex('timestamp', 'timestamp', { unique: false });
                    opsStore.createIndex('type', 'type', { unique: false });
                    console.log('[IndexedDB] Created pending operations store');
                }

                // Create sync metadata store
                if (!db.objectStoreNames.contains(STORES.SYNC_METADATA)) {
                    db.createObjectStore(STORES.SYNC_METADATA, { keyPath: 'key' });
                    console.log('[IndexedDB] Created sync metadata store');
                }
            };
        });
    }

    // Ensure database is initialized
    async function ensureDB() {
        if (!db) {
            await initDB();
        }
        return db;
    }

    // Save an event to local storage
    async function saveEvent(event) {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.EVENTS], 'readwrite');
            const store = transaction.objectStore(STORES.EVENTS);

            // Add sync metadata
            const eventWithMetadata = {
                ...event,
                lastModified: new Date().toISOString(),
                syncStatus: 'pending'
            };

            const request = store.put(eventWithMetadata);

            request.onsuccess = () => {
                console.log('[IndexedDB] Event saved:', event.id);
                resolve(eventWithMetadata);
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error saving event:', request.error);
                reject(request.error);
            };
        });
    }

    // Get all events
    async function getAllEvents(userId) {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.EVENTS], 'readonly');
            const store = transaction.objectStore(STORES.EVENTS);
            const index = store.index('userId');
            const request = userId ? index.getAll(userId) : store.getAll();

            request.onsuccess = () => {
                console.log('[IndexedDB] Retrieved events:', request.result.length);
                resolve(request.result);
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error getting events:', request.error);
                reject(request.error);
            };
        });
    }

    // Get a single event by ID
    async function getEvent(eventId) {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.EVENTS], 'readonly');
            const store = transaction.objectStore(STORES.EVENTS);
            const request = store.get(eventId);

            request.onsuccess = () => {
                resolve(request.result);
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error getting event:', request.error);
                reject(request.error);
            };
        });
    }

    // Delete an event
    async function deleteEvent(eventId) {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.EVENTS], 'readwrite');
            const store = transaction.objectStore(STORES.EVENTS);
            const request = store.delete(eventId);

            request.onsuccess = () => {
                console.log('[IndexedDB] Event deleted:', eventId);
                resolve();
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error deleting event:', request.error);
                reject(request.error);
            };
        });
    }

    // Update event dates (for drag and drop offline)
    async function updateEventDates(eventId, startDate, endDate, allDay) {
        const database = await ensureDB();
        return new Promise(async (resolve, reject) => {
            try {
                // First, get the existing event
                const event = await getEvent(eventId);
                if (!event) {
                    reject(new Error(`Event ${eventId} not found`));
                    return;
                }

                // Update the dates
                event.startDate = startDate;
                event.endDate = endDate;
                event.isAllDay = allDay;
                event.lastModified = new Date().toISOString();
                event.syncStatus = 'pending';

                // Save the updated event
                const transaction = database.transaction([STORES.EVENTS], 'readwrite');
                const store = transaction.objectStore(STORES.EVENTS);
                const request = store.put(event);

                request.onsuccess = () => {
                    console.log('[IndexedDB] Event dates updated:', eventId);
                    resolve(event);
                };

                request.onerror = () => {
                    console.error('[IndexedDB] Error updating event dates:', request.error);
                    reject(request.error);
                };
            } catch (error) {
                reject(error);
            }
        });
    }

    // Save a pending operation
    async function savePendingOperation(operation) {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.PENDING_OPERATIONS], 'readwrite');
            const store = transaction.objectStore(STORES.PENDING_OPERATIONS);

            const opWithTimestamp = {
                ...operation,
                timestamp: new Date().toISOString()
            };

            const request = store.add(opWithTimestamp);

            request.onsuccess = () => {
                console.log('[IndexedDB] Pending operation saved:', request.result);
                resolve(request.result);
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error saving pending operation:', request.error);
                reject(request.error);
            };
        });
    }

    // Get all pending operations
    async function getPendingOperations() {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.PENDING_OPERATIONS], 'readonly');
            const store = transaction.objectStore(STORES.PENDING_OPERATIONS);
            const request = store.getAll();

            request.onsuccess = () => {
                console.log('[IndexedDB] Retrieved pending operations:', request.result.length);
                resolve(request.result);
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error getting pending operations:', request.error);
                reject(request.error);
            };
        });
    }

    // Delete a pending operation
    async function deletePendingOperation(operationId) {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.PENDING_OPERATIONS], 'readwrite');
            const store = transaction.objectStore(STORES.PENDING_OPERATIONS);
            const request = store.delete(operationId);

            request.onsuccess = () => {
                console.log('[IndexedDB] Pending operation deleted:', operationId);
                resolve();
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error deleting pending operation:', request.error);
                reject(request.error);
            };
        });
    }

    // Clear all pending operations
    async function clearPendingOperations() {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.PENDING_OPERATIONS], 'readwrite');
            const store = transaction.objectStore(STORES.PENDING_OPERATIONS);
            const request = store.clear();

            request.onsuccess = () => {
                console.log('[IndexedDB] All pending operations cleared');
                resolve();
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error clearing pending operations:', request.error);
                reject(request.error);
            };
        });
    }

    // Save sync metadata
    async function saveSyncMetadata(key, value) {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.SYNC_METADATA], 'readwrite');
            const store = transaction.objectStore(STORES.SYNC_METADATA);
            const request = store.put({ key, value, timestamp: new Date().toISOString() });

            request.onsuccess = () => {
                resolve();
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error saving sync metadata:', request.error);
                reject(request.error);
            };
        });
    }

    // Get sync metadata
    async function getSyncMetadata(key) {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction([STORES.SYNC_METADATA], 'readonly');
            const store = transaction.objectStore(STORES.SYNC_METADATA);
            const request = store.get(key);

            request.onsuccess = () => {
                resolve(request.result?.value);
            };

            request.onerror = () => {
                console.error('[IndexedDB] Error getting sync metadata:', request.error);
                reject(request.error);
            };
        });
    }

    // Clear all data (for testing/logout)
    async function clearAllData() {
        const database = await ensureDB();
        return new Promise((resolve, reject) => {
            const transaction = database.transaction(
                [STORES.EVENTS, STORES.PENDING_OPERATIONS, STORES.SYNC_METADATA], 
                'readwrite'
            );

            const promises = [
                new Promise((res, rej) => {
                    const req = transaction.objectStore(STORES.EVENTS).clear();
                    req.onsuccess = res;
                    req.onerror = rej;
                }),
                new Promise((res, rej) => {
                    const req = transaction.objectStore(STORES.PENDING_OPERATIONS).clear();
                    req.onsuccess = res;
                    req.onerror = rej;
                }),
                new Promise((res, rej) => {
                    const req = transaction.objectStore(STORES.SYNC_METADATA).clear();
                    req.onsuccess = res;
                    req.onerror = rej;
                })
            ];

            Promise.all(promises)
                .then(() => {
                    console.log('[IndexedDB] All data cleared');
                    resolve();
                })
                .catch(reject);
        });
    }

    // Public API
    return {
        initDB,
        saveEvent,
        getAllEvents,
        getEvent,
        deleteEvent,
        updateEventDates,
        savePendingOperation,
        getPendingOperations,
        deletePendingOperation,
        clearPendingOperations,
        saveSyncMetadata,
        getSyncMetadata,
        clearAllData
    };
})();

// Initialize on load
console.log('[IndexedDB] IndexedDB Manager loaded');
