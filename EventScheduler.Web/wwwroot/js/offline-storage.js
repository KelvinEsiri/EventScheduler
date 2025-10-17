// Offline Storage using IndexedDB for EventScheduler
// Provides persistent storage for events and pending operations when offline

window.offlineStorage = (function() {
    let db = null;
    const DB_VERSION = 1;
    
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
                    database.createObjectStore(eventsStore, { keyPath: 'id' });
                    console.log('Created events object store');
                }
                
                if (!database.objectStoreNames.contains(pendingStore)) {
                    const pendingOpsStore = database.createObjectStore(pendingStore, { keyPath: 'Id' });
                    pendingOpsStore.createIndex('timestamp', 'Timestamp', { unique: false });
                    console.log('Created pending operations object store');
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
                const transaction = db.transaction(['events'], 'readwrite');
                const store = transaction.objectStore('events');
                
                store.clear();
                
                events.forEach(event => {
                    store.put(event);
                });
                
                transaction.oncomplete = () => {
                    console.log(`Saved ${events.length} events to offline storage`);
                    resolve();
                };
                
                transaction.onerror = () => {
                    console.error('Failed to save events:', transaction.error);
                    reject(transaction.error);
                };
            } catch (error) {
                console.error('Error parsing events:', error);
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
    
    function clearAll() {
        return new Promise((resolve, reject) => {
            if (!db) {
                reject('Database not initialized');
                return;
            }
            
            const transaction = db.transaction(['events', 'pendingOperations'], 'readwrite');
            
            transaction.objectStore('events').clear();
            transaction.objectStore('pendingOperations').clear();
            
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
        addPendingOperation,
        getPendingOperations,
        removePendingOperation,
        clearAll
    };
})();
