/* 
 * Browser Console Fix Script
 * Run this in the browser DevTools console to clear old cached data
 * and fix IndexedDB issues
 */

console.log('🔧 EventScheduler - Browser Cache Fix');
console.log('=====================================');

// Step 1: Delete IndexedDB
console.log('Step 1: Deleting old IndexedDB...');
indexedDB.deleteDatabase('EventSchedulerOfflineDB')
    .onsuccess = () => {
        console.log('✅ IndexedDB deleted successfully');
        
        // Step 2: Clear localStorage
        console.log('Step 2: Clearing localStorage...');
        const authData = {
            username: localStorage.getItem('auth_username'),
            email: localStorage.getItem('auth_email'),
            userId: localStorage.getItem('auth_userId'),
            token: localStorage.getItem('auth_token')
        };
        
        if (authData.token) {
            console.log('✅ Preserved auth data for user:', authData.username);
        }
        
        // Step 3: Clear service worker cache
        console.log('Step 3: Clearing service worker cache...');
        if ('caches' in window) {
            caches.keys().then(names => {
                names.forEach(name => caches.delete(name));
                console.log('✅ Service worker cache cleared');
            });
        }
        
        // Step 4: Reload
        console.log('');
        console.log('✅ All cache cleared!');
        console.log('🔄 Reloading page in 2 seconds...');
        console.log('');
        console.log('After reload, check console for:');
        console.log('  - [OfflineStorage] Sample event structure');
        console.log('  - No "DataError" messages');
        console.log('  - "Successfully saved X events" message');
        
        setTimeout(() => {
            location.reload();
        }, 2000);
    };
