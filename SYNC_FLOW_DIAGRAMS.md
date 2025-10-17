# Synchronization Flow Diagrams

This document illustrates the synchronization flows before and after the improvements.

## Creating Event Online - Before Fix

```
User clicks "Create Event"
    ↓
API Call → Server creates event (ID: 123)
    ↓
Response received
    ↓
Event added to UI (ID: 123)  ← FIRST INSTANCE
    ↓
Set lastLocalOperationTime = Now
    ↓
SignalR broadcasts "EventCreated" (ID: 123)
    ↓
Check: (Now - lastLocalOperationTime) < 2 seconds?
    ↓
├─ YES (within 2s) → Skip ✓ No duplicate
└─ NO (>2s due to latency) → Add to UI again (ID: 123)  ← DUPLICATE! ✗
```

**Problem:** Network latency could cause the 2-second window to elapse, resulting in duplicates.

## Creating Event Online - After Fix

```
User clicks "Create Event"
    ↓
API Call → Server creates event (ID: 123)
    ↓
Response received
    ↓
Event added to UI (ID: 123)
    ↓
Add 123 to optimisticallyAddedEventIds  ← TRACK THE ID
    ↓
SignalR broadcasts "EventCreated" (ID: 123)
    ↓
Check: Is 123 in optimisticallyAddedEventIds?
    ↓
├─ YES → Remove from set, Skip adding ✓ No duplicate
└─ NO → Add to UI (from another user/tab) ✓
```

**Solution:** Reliable ID-based tracking eliminates timing-based race conditions.

---

## Creating Event Offline - Before Fix

```
User clicks "Create Event" (OFFLINE)
    ↓
Generate temp ID: -999
    ↓
Add to cache & UI (ID: -999)
    ↓
Queue create operation
    ↓
                Network restored
                    ↓
            Sync operations
                    ↓
        API creates event (ID: 123)
                    ↓
        Refresh all events from server
                    ↓
        Cache now has: [-999, 123]  ← DUPLICATE! ✗
                    ↓
        UI shows both temp and real event
```

**Problem:** Temporary event wasn't removed after sync, causing duplicates.

## Creating Event Offline - After Fix

```
User clicks "Create Event" (OFFLINE)
    ↓
Generate temp ID: -999
    ↓
Add to cache & UI (ID: -999)
    ↓
Queue create operation WITH TempId: -999  ← TRACK TEMP ID
    ↓
                Network restored
                    ↓
            Sync operations
                    ↓
        API creates event (ID: 123)
                    ↓
        Notify: TempId -999 → RealId 123  ← MAP THE IDS
                    ↓
        Remove temp event (-999) from cache
                    ↓
        UI: Replace -999 with 123 ✓
                    ↓
        SignalR broadcasts "EventCreated" (ID: 123)
                    ↓
        Check tempIdToRealIdMap: -999 → 123
                    ↓
        Skip (we already replaced it) ✓
                    ↓
        Cache has: [123]  ← CLEAN! ✓
```

**Solution:** Temp-to-real ID mapping with automatic cleanup prevents duplicates.

---

## Updating Temporary Event - Before Fix

```
User creates event offline (ID: -999)
    ↓
Queue: [CREATE(-999)]
    ↓
User updates the event
    ↓
Queue: [CREATE(-999), UPDATE(-999)]  ← 2 operations
    ↓
                Network restored
                    ↓
        Sync CREATE → Server creates ID: 123
                    ↓
        Sync UPDATE(-999) → ERROR! Event -999 doesn't exist ✗
```

**Problem:** Update operation references non-existent temporary ID on server.

## Updating Temporary Event - After Fix

```
User creates event offline (ID: -999)
    ↓
Queue: [CREATE(-999, data: {title: "Meeting"})]
    ↓
User updates the event
    ↓
Check: Is -999 a temporary ID? YES
    ↓
Find pending CREATE operation for -999
    ↓
Merge update into CREATE data: {title: "Team Meeting"}  ← SMART MERGE
    ↓
Queue: [CREATE(-999, data: {title: "Team Meeting"})]  ← Still 1 operation
    ↓
                Network restored
                    ↓
        Sync CREATE → Server creates with merged data ✓
```

**Solution:** Updates on temporary events merge into pending creates, reducing operations.

---

## Deleting Temporary Event - Before Fix

```
User creates event offline (ID: -999)
    ↓
Queue: [CREATE(-999)]
    ↓
User deletes the event
    ↓
Queue: [CREATE(-999), DELETE(-999)]  ← 2 operations
    ↓
                Network restored
                    ↓
        Sync CREATE → Server creates ID: 123
                    ↓
        Sync DELETE(-999) → ERROR! Event -999 doesn't exist ✗
```

**Problem:** Delete operation references non-existent temporary ID.

## Deleting Temporary Event - After Fix

```
User creates event offline (ID: -999)
    ↓
Queue: [CREATE(-999)]
    ↓
User deletes the event
    ↓
Check: Is -999 a temporary ID? YES
    ↓
Find and REMOVE pending CREATE operation
    ↓
Queue: []  ← EMPTY! Operations cancelled out ✓
    ↓
                Network restored
                    ↓
        No operations to sync ✓
```

**Solution:** Deleting temporary events removes pending creates entirely.

---

## Multi-Client Synchronization - After Fix

```
TAB 1: Create event                    TAB 2: Listening
    ↓                                       ↓
API creates (ID: 123)                  SignalR connected
    ↓                                       ↓
Add to UI (ID: 123)                    Receives "EventCreated" (ID: 123)
    ↓                                       ↓
Track in optimisticallyAddedEventIds   Check: Not in our optimisticallyAddedEventIds
    ↓                                       ↓
SignalR "EventCreated" (ID: 123)       Add to UI with notification ✓
    ↓                                       
Skip (in our optimisticallyAddedEventIds)  "Event created by another user"
    ↓
No duplicate ✓

RESULT: Tab 1 has event without notification (their own action)
        Tab 2 has event with notification (from another tab)
```

---

## Complete Flow Chart - Offline to Online Transition

```
┌─────────────────────────────────────────────────────────────────┐
│                        OFFLINE MODE                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  User Action → Generate Temp ID (-999)                         │
│       ↓                                                         │
│  Store in IndexedDB:                                           │
│    - events: [{id: -999, title: "Meeting"}]                   │
│    - pendingOperations: [{type: "create", TempId: -999}]      │
│       ↓                                                         │
│  Update UI (show temp event)                                   │
│       ↓                                                         │
│  Update counter: "1 pending operation"                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│                   NETWORK RESTORED                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Network Status: ONLINE                                         │
│       ↓                                                         │
│  Trigger: Auto-sync                                            │
│       ↓                                                         │
│  Read from IndexedDB:                                          │
│    - pendingOperations: [{type: "create", TempId: -999}]      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│                      SYNC PROCESS                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  For each pending operation:                                    │
│    ↓                                                            │
│  1. Send to API → Response: {id: 123, title: "Meeting"}       │
│    ↓                                                            │
│  2. Map IDs: tempIdToRealIdMap[-999] = 123                     │
│    ↓                                                            │
│  3. Notify UI: OnTempIdMapped(-999, 123)                       │
│    ↓                                                            │
│  4. UI: Remove temp event (-999), Add real event (123)         │
│    ↓                                                            │
│  5. Remove from pendingOperations                               │
│    ↓                                                            │
│  6. Remove temp event from cache                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│                    POST-SYNC CLEANUP                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Reload all events from server                                  │
│    ↓                                                            │
│  Update cache with fresh server data                            │
│    ↓                                                            │
│  Clear pending operations counter                               │
│    ↓                                                            │
│  IndexedDB final state:                                         │
│    - events: [{id: 123, title: "Meeting"}]  ← Real event only │
│    - pendingOperations: []  ← Empty                            │
│    ↓                                                            │
│  Show success: "Synced 1 change successfully"                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## State Diagram - Event Lifecycle

```
┌──────────────┐
│   CREATED    │  Online: Real ID (positive)
│   ONLINE     │  Offline: Temp ID (negative)
└──────┬───────┘
       │
       ├─────────────┐
       │             │
   ┌───▼────┐    ┌──▼──────────┐
   │ CACHED │    │ IN UI       │
   │  (DB)  │    │ (Rendered)  │
   └───┬────┘    └──┬──────────┘
       │             │
       │          ┌──▼──────────┐
       │          │ OPTIMISTIC  │  Tracked in optimisticallyAddedEventIds
       │          │   ADDED     │  (online create only)
       │          └──┬──────────┘
       │             │
   ┌───▼────────────▼───┐
   │  SIGNALR BROADCAST │
   └───┬────────────────┘
       │
       ├──[Same ID in optimisticallyAddedEventIds?]
       │
       ├─ YES → SKIP (own event)
       │
       └─ NO ──[Event exists in list?]
               │
               ├─ YES → UPDATE existing
               │
               └─ NO → ADD new (from other user/tab)

TEMPORARY EVENTS:
┌──────────────┐
│   CREATED    │
│  OFFLINE     │  Temp ID: negative
└──────┬───────┘
       │
   ┌───▼────────────────┐
   │ PENDING OPERATION  │  Stored with TempId
   └───┬────────────────┘
       │
   ┌───▼────────────────┐
   │   NETWORK UP       │
   └───┬────────────────┘
       │
   ┌───▼────────────────┐
   │   SYNC TO API      │
   └───┬────────────────┘
       │
   ┌───▼────────────────┐
   │   GET REAL ID      │  Map: TempId → RealId
   └───┬────────────────┘
       │
   ┌───▼────────────────┐
   │  REPLACE IN UI     │  Remove temp, add real
   └───┬────────────────┘
       │
   ┌───▼────────────────┐
   │  CLEANUP CACHE     │  Remove temp from DB
   └────────────────────┘
```

---

## Summary of Key Improvements

### 1. Duplicate Prevention
**Mechanism:** ID-based tracking instead of time-based  
**Benefit:** 100% reliable, no timing issues

### 2. Temp ID Mapping
**Mechanism:** Dictionary mapping negative to positive IDs  
**Benefit:** Seamless transitions, automatic cleanup

### 3. Smart Operations
**Mechanism:** Merge/cancel operations on temp events  
**Benefit:** Fewer API calls, cleaner queue

### 4. Cache Management
**Mechanism:** Automatic cleanup after sync  
**Benefit:** Consistent state, no orphaned data

### 5. Multi-Client Sync
**Mechanism:** Proper event source tracking  
**Benefit:** Correct notifications, no spam

---

**Visual Legend:**
- `✓` = Working correctly
- `✗` = Bug/Issue (before fix)
- `←` = Key improvement point
- `→` = Data flow
- `↓` = Process step

