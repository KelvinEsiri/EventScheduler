# Offline Mode Troubleshooting Guide

## Table of Contents
1. [Quick Diagnostics](#quick-diagnostics)
2. [Common Issues](#common-issues)
3. [Error Messages Explained](#error-messages-explained)
4. [Recovery Procedures](#recovery-procedures)
5. [Advanced Debugging](#advanced-debugging)

---

## Quick Diagnostics

### Run This First
```javascript
// Copy-paste into browser console
(async function diagnose() {
    console.log('=== OFFLINE MODE DIAGNOSTICS ===\n');
    
    // 1. Check online status
    console.log('1. ONLINE STATUS:');
    console.log('   Browser (navigator.onLine):', navigator.onLine);
    console.log('   Connectivity Manager:', window.connectivityManager?.isOnline);
    console.log('   Blazor Connected:', window.fullCalendarInterop?.isBlazorConnected);
    console.log('   Blazor Available:', !!window.Blazor);
    
    // 2. Check pending operations
    console.log('\n2. PENDING OPERATIONS:');
    try {
        const ops = await window.indexedDBManager.getPendingOperations();
        console.log('   Count:', ops.length);
        if (ops.length > 0) {
            console.log('   First operation:', ops[0]);
            console.log('   Date format check:', ops[0]?.data?.startDate);
            console.log('   ✓ ISO 8601?', /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.test(ops[0]?.data?.startDate));
        }
    } catch (e) {
        console.log('   ERROR:', e.message);
    }
    
    // 3. Check events
    console.log('\n3. CACHED EVENTS:');
    try {
        const events = await window.indexedDBManager.getAllEvents();
        console.log('   Count:', events.length);
        if (events.length > 0) {
            console.log('   Sample event:', events[0]);
        }
    } catch (e) {
        console.log('   ERROR:', e.message);
    }
    
    // 4. Check versions
    console.log('\n4. SCRIPT VERSIONS:');
    const scripts = document.querySelectorAll('script[src*="?v="]');
    scripts.forEach(s => {
        const match = s.src.match(/([^\/]+)\?v=(\d+)/);
        if (match) {
            console.log(`   ${match[1]}: v${match[2]}`);
        }
    });
    
    console.log('\n=== DIAGNOSTICS COMPLETE ===');
})();
```

---

## Common Issues

### Issue #1: Events Don't Save Offline

**Symptoms**:
- Drag event while offline
- Event bounces back to original position
- No console messages about offline save

**Diagnosis**:
```javascript
// Run in console
console.log('Check connection detection:');
console.log('Blazor connected?', window.fullCalendarInterop?.isBlazorConnected);
console.log('Browser online?', navigator.onLine);
```

**Possible Causes & Fixes**:

#### Cause A: Connection not detected as offline
```javascript
// Check if connectivity manager is initialized
console.log('Manager exists?', !!window.connectivityManager);
console.log('Manager online?', window.connectivityManager?.isOnline);

// Fix: Wait longer for offline detection
// Go offline, then wait 5 seconds before dragging
```

#### Cause B: Blazor connection flag stuck at true
```javascript
// Check flag
console.log(window.fullCalendarInterop.isBlazorConnected);

// Quick fix
window.fullCalendarInterop.isBlazorConnected = false;
```

#### Cause C: IndexedDB not initialized
```javascript
// Check IndexedDB
console.log('IndexedDB manager?', !!window.indexedDBManager);

// Try to open manually
await window.indexedDBManager.initDB();
```

---

### Issue #2: 400 Bad Request During Sync

**Symptoms**:
- Go back online
- Page reloads
- Console shows: `400 Bad Request`
- Network tab shows failed PUT request

**Diagnosis**:
```javascript
// Check pending operation date format
const ops = await window.indexedDBManager.getPendingOperations();
console.log('Date format:', ops[0]?.data?.startDate);

// Expected: "2025-10-26T09:00:00.000Z" (ISO 8601)
// Wrong: "2025-10-26" (date only)
```

**Fix**:
```javascript
// Clear old operations with wrong format
await window.indexedDBManager.clearPendingOperations();
console.log('Old operations cleared. Re-do your changes.');

// Then refresh page
location.reload();
```

**Prevention**:
Ensure `fullcalendar-interop.js` uses `.toISOString()`:
```javascript
const startDate = info.event.start.toISOString();  // ✅ Correct
const startDate = info.event.startStr;             // ❌ Wrong
```

---

### Issue #3: Page Freezes When Going Offline

**Symptoms**:
- Go offline
- Spinner appears
- Page completely unresponsive
- Can't click anything

**Diagnosis**:
```javascript
// Check reconnection handler
console.log('Reconnection handler installed?', 
    window.Blazor?.defaultReconnectionHandler?.onConnectionDown !== undefined
);
```

**Cause**: Blazor's default reconnection handler being called

**Fix**: Check `reconnection-handler.js` line 52-86:

```javascript
// ❌ WRONG - This causes freeze
handler.onConnectionDown = function() {
    modal.className = 'components-reconnect-hide';
    if (origDown) origDown.call(handler);  // ← REMOVE THIS!
};

// ✅ CORRECT - No freeze
handler.onConnectionDown = function() {
    modal.className = 'components-reconnect-hide';
    window.fullCalendarInterop.isBlazorConnected = false;
    // DON'T call origDown!
};
```

**Immediate Recovery**:
1. Hard refresh: Ctrl+Shift+R
2. Check file version: `reconnection-handler.js?v=14` or higher

---

### Issue #4: Clicks Don't Work After Coming Back Online

**Symptoms**:
- Was offline, now online
- Page reloaded
- Click events → shows offline alert (wrong!)
- Should show edit modal

**Diagnosis**:
```javascript
// Check if online state restored
console.log({
    navigator: navigator.onLine,
    manager: window.connectivityManager?.isOnline,
    blazor: window.fullCalendarInterop?.isBlazorConnected
});

// If blazor is false but others are true → Problem!
```

**Fix**:
```javascript
// Quick fix - restore flag
window.fullCalendarInterop.isBlazorConnected = true;

// Then click event again
```

**Permanent Fix**: 
Ensure `fullcalendar-interop.js` `checkBlazorConnection()` has auto-restore:
```javascript
// Lines 9-40
if (!this.isBlazorConnected && navigator.onLine) {
    console.log('🔌 Connection restored - re-enabling Blazor calls');
    this.isBlazorConnected = true;  // ← Must have this!
}
```

---

### Issue #5: 405 Method Not Allowed During Sync

**Symptoms**:
- Sync fails with 405 error
- Network tab shows wrong URL or method

**Diagnosis**:
```javascript
// Check pending operation endpoint
const ops = await window.indexedDBManager.getPendingOperations();
console.log('Endpoint:', ops[0]?.endpoint);
console.log('Method:', ops[0]?.type);

// Should be: "/api/events/21" or "http://localhost:5006/api/events/21"
```

**Cause**: Service worker not constructing full URL

**Fix**: Check `service-worker.js` line 195:
```javascript
// ✅ CORRECT
const apiBaseUrl = 'http://localhost:5006';
const fullUrl = endpoint.startsWith('http') 
    ? endpoint 
    : `${apiBaseUrl}${endpoint}`;

const response = await fetch(fullUrl, options);

// ❌ WRONG
const response = await fetch(operation.endpoint, options);
```

---

### Issue #6: Sync Never Happens After Coming Online

**Symptoms**:
- Go offline, drag events
- Come back online
- Page reloads
- Events back in old position (not synced)
- Pending operations still in IndexedDB

**Diagnosis**:
```javascript
// Check if sync service called
// Open server logs and look for:
// "CalendarView: Auto-sync completed"

// Check if pending operations exist
const ops = await window.indexedDBManager.getPendingOperations();
console.log('Still pending:', ops.length);
```

**Possible Causes**:

#### Cause A: Sync service not triggered
```csharp
// Check CalendarView.razor.cs OnConnectivityChanged
private void OnConnectivityChanged(object? sender, bool online)
{
    if (online)
    {
        // Must call sync!
        var result = await SyncService.SyncAsync();
    }
}
```

#### Cause B: Auth token missing
```javascript
// Check token
console.log('Token exists?', !!localStorage.getItem('auth_token'));

// If missing, login again
```

#### Cause C: API server not running
```bash
# Check if API is running
curl http://localhost:5006/api/events

# Should return 200 OK or 401 Unauthorized
# NOT connection refused
```

---

## Error Messages Explained

### "Cannot send data if the connection is not in the 'Connected' State"

**Type**: Expected during offline mode  
**Severity**: Low (handled by offline fallback)  
**Action**: None - this is normal

**What it means**: Blazor circuit is disconnected, can't call C# methods

**How it's handled**:
```javascript
Promise.race([blazorCall, timeout])
    .catch(err => {
        // This error triggers offline fallback
        if (err.message.includes('not in the \'Connected\' State')) {
            // Save offline instead
        }
    });
```

---

### "JavaScript interop calls cannot be issued...statically rendered"

**Type**: Warning during prerendering  
**Severity**: Low (harmless)  
**Action**: None - expected behavior

**What it means**: Component is prerendering server-side, JavaScript not available yet

**When it happens**:
- Page first loads
- Page reloads after reconnection

**Why it's harmless**: Services retry after render completes

---

### "The list of component operations is not valid"

**Type**: Error during reconnection  
**Severity**: Medium (breaks reconnection)  
**Action**: Force page reload

**What it means**: Blazor circuit has invalid state after reconnection

**Fix**:
```javascript
// Ensure reconnection handler reloads page
// reconnection-handler.js line 88-96
handler.onConnectionUp = function() {
    window.location.href = url.toString();  // Force reload
};
```

---

### "Failed to update event" (in UI modal)

**Type**: User-facing error  
**Severity**: High if wrong, Low if correct  
**Action**: Check if actually failed

**Check**:
```javascript
// Look for this in console:
// "✅ Event saved offline successfully"

// If you see that, it's a FALSE error!
```

**Fix**: Wrap `SaveEvent()` with `JSDisconnectedException` handling:
```csharp
catch (JSDisconnectedException)
{
    ShowSuccess("Event saved offline - will sync when online");
    // Don't show error!
}
```

---

## Recovery Procedures

### Emergency Reset

```javascript
// ⚠️ WARNING: This clears ALL offline data!
// Only use if totally broken

(async function emergencyReset() {
    console.log('Starting emergency reset...');
    
    // 1. Clear pending operations
    await window.indexedDBManager.clearPendingOperations();
    console.log('✓ Pending operations cleared');
    
    // 2. Clear sync metadata
    await window.indexedDBManager.clearAllData();
    console.log('✓ All IndexedDB data cleared');
    
    // 3. Reset flags
    if (window.fullCalendarInterop) {
        window.fullCalendarInterop.isBlazorConnected = true;
        console.log('✓ Blazor connection flag reset');
    }
    
    // 4. Hard reload
    console.log('Reloading page...');
    setTimeout(() => {
        window.location.href = window.location.origin + window.location.pathname + '?reset=' + Date.now();
    }, 1000);
})();
```

---

### Soft Reset (Keep Events)

```javascript
// Clear pending operations but keep cached events
(async function softReset() {
    console.log('Starting soft reset...');
    
    // Only clear pending operations
    await window.indexedDBManager.clearPendingOperations();
    console.log('✓ Pending operations cleared (events preserved)');
    
    // Reset flags
    if (window.fullCalendarInterop) {
        window.fullCalendarInterop.isBlazorConnected = navigator.onLine;
        console.log('✓ Connection flags reset');
    }
    
    // Reload
    location.reload();
})();
```

---

### Repair Pending Operations

```javascript
// Fix pending operations with wrong date format
(async function repairOperations() {
    console.log('Checking pending operations...');
    
    const ops = await window.indexedDBManager.getPendingOperations();
    console.log(`Found ${ops.length} operations`);
    
    let fixed = 0;
    for (const op of ops) {
        // Check if date is in wrong format
        const startDate = op.data?.startDate;
        if (startDate && !startDate.includes('T')) {
            console.log(`⚠️ Operation ${op.id} has wrong date format:`, startDate);
            console.log('   This operation will fail during sync!');
            
            // Can't auto-fix - dates need original time info
            // Best to delete and re-do
        } else if (startDate) {
            console.log(`✓ Operation ${op.id} has correct date format`);
            fixed++;
        }
    }
    
    if (fixed < ops.length) {
        console.log('\n⚠️ RECOMMENDATION: Clear pending operations and re-do changes');
        console.log('   Run: await window.indexedDBManager.clearPendingOperations()');
    } else {
        console.log('\n✓ All operations have correct format!');
    }
})();
```

---

## Advanced Debugging

### Enable Debug Mode

```javascript
// Add to localStorage
localStorage.setItem('DEBUG_OFFLINE', 'true');

// Reload page
location.reload();

// Now all offline operations log verbosely
```

### Monitor IndexedDB Changes

```javascript
// Watch for IndexedDB changes
const observer = {
    start() {
        const original = window.indexedDBManager.savePendingOperation;
        window.indexedDBManager.savePendingOperation = async function(...args) {
            console.log('📝 Saving pending operation:', args[0]);
            const result = await original.apply(this, args);
            console.log('✓ Saved with ID:', result);
            return result;
        };
        console.log('✓ IndexedDB monitor active');
    }
};

observer.start();
```

### Network Simulation

```javascript
// Simulate offline mode programmatically
function simulateOffline() {
    window.fullCalendarInterop.isBlazorConnected = false;
    window.connectivityManager.isOnline = false;
    console.log('✓ Offline mode simulated');
}

function simulateOnline() {
    window.fullCalendarInterop.isBlazorConnected = true;
    // Note: Can't change connectivityManager.isOnline (private var)
    console.log('✓ Online mode simulated (partial)');
}

// Usage
simulateOffline();
// ... drag events ...
simulateOnline();
```

### Trace Execution Flow

```javascript
// Add console.trace() to track where code is called from
const original = window.fullCalendarInterop.handleOfflineEventDrop;
window.fullCalendarInterop.handleOfflineEventDrop = function(...args) {
    console.trace('handleOfflineEventDrop called');
    return original.apply(this, args);
};
```

---

## Performance Profiling

### Measure Offline Save Time

```javascript
async function profileOfflineSave() {
    const eventId = 21; // Change to your event ID
    const testData = {
        title: 'Test Event',
        startDate: new Date().toISOString(),
        endDate: new Date(Date.now() + 3600000).toISOString()
    };
    
    console.time('IndexedDB Save');
    await window.indexedDBManager.updateEventDates(
        eventId, 
        testData.startDate, 
        testData.endDate, 
        false
    );
    console.timeEnd('IndexedDB Save');
    
    console.time('Queue Operation');
    await window.indexedDBManager.savePendingOperation({
        type: 'PUT',
        endpoint: `/api/events/${eventId}`,
        data: testData,
        token: localStorage.getItem('auth_token'),
        timestamp: new Date().toISOString()
    });
    console.timeEnd('Queue Operation');
}

profileOfflineSave();
// Expected: < 50ms total
```

---

## Validation Checklist

Run before deploying offline mode changes:

```javascript
(async function validate() {
    console.log('=== OFFLINE MODE VALIDATION ===\n');
    
    const checks = {
        'File Versions': {
            'fullcalendar-interop.js': 15,
            'reconnection-handler.js': 14,
            'connectivity-manager.js': 3
        },
        'Functions Exist': [
            'window.fullCalendarInterop.checkBlazorConnection',
            'window.fullCalendarInterop.handleOfflineEventDrop',
            'window.connectivityManager.isOnline',
            'window.indexedDBManager.savePendingOperation'
        ],
        'Connection Checks': [
            'navigator.onLine',
            'window.Blazor'
        ]
    };
    
    // Check versions
    console.log('1. Checking versions...');
    for (const [file, expectedVersion] of Object.entries(checks['File Versions'])) {
        const script = document.querySelector(`script[src*="${file}"]`);
        const match = script?.src.match(/\?v=(\d+)/);
        const version = match ? parseInt(match[1]) : 0;
        const status = version >= expectedVersion ? '✓' : '✗';
        console.log(`   ${status} ${file}: v${version} (expected: v${expectedVersion})`);
    }
    
    // Check functions
    console.log('\n2. Checking functions...');
    for (const path of checks['Functions Exist']) {
        const exists = !!eval(path);
        const status = exists ? '✓' : '✗';
        console.log(`   ${status} ${path}`);
    }
    
    // Check connection
    console.log('\n3. Checking connection...');
    for (const check of checks['Connection Checks']) {
        const value = eval(check);
        console.log(`   ✓ ${check}:`, value);
    }
    
    console.log('\n=== VALIDATION COMPLETE ===');
})();
```

---

**Need More Help?**

1. Check `OFFLINE_MODE_COMPREHENSIVE_GUIDE.md` for detailed explanations
2. Review `OFFLINE_MODE_QUICK_REFERENCE.md` for code patterns
3. Check browser console for specific error messages
4. Verify IndexedDB state in DevTools
5. Check server logs for sync-related messages

---

**Last Updated**: October 18, 2025  
**Troubleshooting Guide Version**: 1.0
