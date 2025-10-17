// Offline Storage using IndexedDB for EventScheduler
// Handles persistent storage for events, pending operations, and conflict tracking

window.offlineStorage = (function() {
    let db = null;
    const DB_VERSION = 2;
    
    function initDB(dbName, eventsStore, pendingStore) {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(dbName, DB_VERSION);
            
            request.onerror = () => {
                console.error('Failed to open IndexedDB:', request.error);
                reject(request.error);
            };
            
            request.onsuccess = () => {
                db = request.result;
                console.log('IndexedDB initialized successfully');
                resolve();
            };
            
            request.onupgradeneeded = (event) => {
                const database = event.target.result;
                
                if (!database.objectStoreNames.contains(eventsStore)) {
                    const eventsObjStore = database.createObjectStore(eventsStore, { keyPath: 'id' });
                    eventsObjStore.createIndex('lastModified', 'lastModified', { unique: false });
                    console.log('Created events object store');
                }
                
                if (!database.objectStoreNames.contains(pendingStore)) {
                    const pendingOpsStore = database.createObjectStore(pendingStore, { keyPath: 'Id' });
                    pendingOpsStore.createIndex('timestamp', 'Timestamp', { unique: false });
                    pendingOpsStore.createIndex('eventId', 'EventId', { unique: false });
                    console.log('Created pending operations object store');
                }
                
                if (!database.objectStoreNames.contains('conflicts')) {
                    const conflictsStore = database.createObjectStore('conflicts', { 
                        keyPath: 'id', 
                        autoIncrement: true 
                    });
                    conflictsStore.createIndex('eventId', 'eventId', { unique: false });
                    conflictsStore.createIndex('timestamp', 'timestamp', { unique: false });
                    console.log('Created conflicts object store');
                }
            };
        });
    }
    
    function saveEvents(eventsJson) {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            try {
                const events = JSON.parse(eventsJson);
                console.log(`[OfflineStorage] Attempting to save ${events.length} events`);
                
                // Log first event to check structure
                if (events.length > 0) {
                    console.log('[OfflineStorage] Sample event structure:', {
                        hasLowercaseId: 'id' in events[0],
                        hasUppercaseId: 'Id' in events[0],
                        idValue: events[0].id || events[0].Id,
                        keys: Object.keys(events[0])
                    });
                }
                
                // Normalize all events BEFORE starting transaction
                const normalizedEvents = events.map((event, index) => {
                    // Use JSON parse/stringify for deep clone (handles all properties)
                    const normalized = JSON.parse(JSON.stringify(event));
                    
                    // Handle C# Id -> id conversion
                    if (!normalized.id && normalized.Id) {
                        normalized.id = normalized.Id;
                    }
                    
                    // Ensure id exists and is valid
                    if (!normalized.id || normalized.id === null || normalized.id === undefined) {
                        console.error('[OfflineStorage] Event missing valid id after normalization:', {
                            original: event,
                            normalized: normalized
                        });
                        return null;
                    }
                    
                    // Ensure id is a number
                    normalized.id = Number(normalized.id);
                    
                    // Remove uppercase Id to avoid confusion
                    delete normalized.Id;
                    
                    console.log(`[OfflineStorage] Event ${index + 1} normalized: id=${normalized.id}, title="${normalized.title}"`);
                    
                    return normalized;
                }).filter(e => e !== null); // Remove nulls
                
                console.log(`[OfflineStorage] Normalized ${normalizedEvents.length} events for storage`);
                
                const transaction = db.transaction(['events'], 'readwrite');
                const store = transaction.objectStore('events');
                
                store.clear();
                
                let savedCount = 0;
                let skippedCount = 0;
                
                normalizedEvents.forEach((event, index) => {
                    try {
                        const request = store.put(event);
                        request.onsuccess = () => {
                            savedCount++;
                            console.log(`[OfflineStorage] ✓ Saved event ${event.id}`);
                        };
                        request.onerror = (err) => {
                            console.error(`[OfflineStorage] ✗ Failed to save event ${event.id}:`, err);
                            skippedCount++;
                        };
                    } catch (err) {
                        console.error(`[OfflineStorage] ✗ Exception saving event ${event.id}:`, err);
                        skippedCount++;
                    }
                });
                
                transaction.oncomplete = () => {
                    console.log(`[OfflineStorage] Successfully saved ${normalizedEvents.length} events`);
                    resolve();
                };
                
                transaction.onerror = () => {
                    console.error('[OfflineStorage] Transaction error:', transaction.error);
                    reject(transaction.error);
                };
            } catch (error) {
                console.error('[OfflineStorage] Error in saveEvents:', error);
                reject(error);
            }
        });
    }
    
    function getEvents() {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            const transaction = db.transaction(['events'], 'readonly');
            const store = transaction.objectStore('events');
            const request = store.getAll();
            
            request.onsuccess = () => {
                const events = request.result || [];
                console.log(`Retrieved ${events.length} events from offline storage`);
                resolve(JSON.stringify(events));
            };
            
            request.onerror = () => {
                console.error('Failed to get events:', request.error);
                reject(request.error);
            };
        });
    }
    
    function addPendingOperation(operationJson) {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            try {
                const operation = JSON.parse(operationJson);
                const transaction = db.transaction(['pendingOperations'], 'readwrite');
                const store = transaction.objectStore('pendingOperations');
                
                store.add(operation);
                
                transaction.oncomplete = () => {
                    console.log(`Added pending operation: ${operation.Type} for event ${operation.EventId}`);
                    resolve();
                };
                
                transaction.onerror = () => {
                    console.error('Failed to add pending operation:', transaction.error);
                    reject(transaction.error);
                };
            } catch (error) {
                console.error('Error parsing operation:', error);
                reject(error);
            }
        });
    }
    
    function getPendingOperations() {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            const transaction = db.transaction(['pendingOperations'], 'readonly');
            const store = transaction.objectStore('pendingOperations');
            const request = store.getAll();
            
            request.onsuccess = () => {
                const operations = request.result || [];
                console.log(`Retrieved ${operations.length} pending operations`);
                resolve(JSON.stringify(operations));
            };
            
            request.onerror = () => {
                console.error('Failed to get pending operations:', request.error);
                reject(request.error);
            };
        });
    }
    
    function removePendingOperation(operationId) {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            const transaction = db.transaction(['pendingOperations'], 'readwrite');
            const store = transaction.objectStore('pendingOperations');
            
            store.delete(operationId);
            
            transaction.oncomplete = () => {
                console.log(`Removed pending operation: ${operationId}`);
                resolve();
            };
            
            transaction.onerror = () => {
                console.error('Failed to remove pending operation:', transaction.error);
                reject(transaction.error);
            };
        });
    }
    
    function saveEventWithTimestamp(eventJson) {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            try {
                const event = JSON.parse(eventJson);
                
                // Ensure the event has an 'id' field (handle C# Id -> id conversion)
                if (!event.id && event.Id) {
                    event.id = event.Id;
                }
                
                if (!event.id) {
                    reject('Event missing id field');
                    return;
                }
                
                event.lastModified = Date.now();
                
                const transaction = db.transaction(['events'], 'readwrite');
                const store = transaction.objectStore('events');
                store.put(event);
                
                transaction.oncomplete = () => {
                    console.log(`Saved event ${event.id} with timestamp`);
                    resolve();
                };
                
                transaction.onerror = () => {
                    console.error('Failed to save event:', transaction.error);
                    reject(transaction.error);
                };
            } catch (error) {
                console.error('Error parsing event:', error);
                reject(error);
            }
        });
    }
    
    function addConflict(conflictJson) {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            try {
                const conflict = JSON.parse(conflictJson);
                conflict.timestamp = Date.now();
                
                const transaction = db.transaction(['conflicts'], 'readwrite');
                const store = transaction.objectStore('conflicts');
                store.add(conflict);
                
                transaction.oncomplete = () => {
                    console.log(`Recorded conflict for event ${conflict.eventId}`);
                    resolve();
                };
                
                transaction.onerror = () => {
                    console.error('Failed to add conflict:', transaction.error);
                    reject(transaction.error);
                };
            } catch (error) {
                console.error('Error parsing conflict:', error);
                reject(error);
            }
        });
    }
    
    function getConflicts() {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            const transaction = db.transaction(['conflicts'], 'readonly');
            const store = transaction.objectStore('conflicts');
            const request = store.getAll();
            
            request.onsuccess = () => {
                const conflicts = request.result || [];
                console.log(`Retrieved ${conflicts.length} conflicts`);
                resolve(JSON.stringify(conflicts));
            };
            
            request.onerror = () => {
                console.error('Failed to get conflicts:', request.error);
                reject(request.error);
            };
        });
    }
    
    function clearConflict(conflictId) {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            const transaction = db.transaction(['conflicts'], 'readwrite');
            const store = transaction.objectStore('conflicts');
            store.delete(conflictId);
            
            transaction.oncomplete = () => {
                console.log(`Cleared conflict ${conflictId}`);
                resolve();
            };
            
            transaction.onerror = () => {
                console.error('Failed to clear conflict:', transaction.error);
                reject(transaction.error);
            };
        });
    }
    
    function getPendingOperationsCount() {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            const transaction = db.transaction(['pendingOperations'], 'readonly');
            const store = transaction.objectStore('pendingOperations');
            const request = store.count();
            
            request.onsuccess = () => {
                resolve(request.result);
            };
            
            request.onerror = () => {
                console.error('Failed to get pending operations count:', request.error);
                reject(request.error);
            };
        });
    }
    
    function clearAll() {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            const storeNames = ['events', 'pendingOperations'];
            if (db.objectStoreNames.contains('conflicts')) {
                storeNames.push('conflicts');
            }
            
            const transaction = db.transaction(storeNames, 'readwrite');
            
            storeNames.forEach(storeName => {
                transaction.objectStore(storeName).clear();
            });
            
            transaction.oncomplete = () => {
                console.log('Cleared all offline data');
                resolve();
            };
            
            transaction.onerror = () => {
                console.error('Failed to clear offline data:', transaction.error);
                reject(transaction.error);
            };
        });
    }
    
    return {
        initDB,
        saveEvents,
        getEvents,
        saveEventWithTimestamp,
        addPendingOperation,
        getPendingOperations,
        removePendingOperation,
        getPendingOperationsCount,
        addConflict,
        getConflicts,
        clearConflict,
        clearAll
    };
})();
