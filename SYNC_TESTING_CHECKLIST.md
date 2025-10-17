# Synchronization Testing Checklist

This document provides a comprehensive testing checklist for the improved offline-first synchronization functionality.

## Prerequisites

1. Start both API and Web servers:
   ```bash
   # Terminal 1 - API Server
   cd EventScheduler.Api
   dotnet run
   
   # Terminal 2 - Web Server
   cd EventScheduler.Web
   dotnet run
   ```

2. Open browser to `http://localhost:5292`
3. Open DevTools (F12) → Console tab
4. Login or register an account

## Test Suite 1: Online Event Creation (Duplicate Prevention)

### Test 1.1: Single Tab - Create Event
- [ ] Create a new event while online
- [ ] Verify event appears immediately in the UI
- [ ] Check console for: `"Event {EventId} added optimistically"`
- [ ] Wait for SignalR broadcast
- [ ] Check console for: `"Skipping SignalR EventCreated - already added optimistically"`
- [ ] **Expected**: Event appears only ONCE (no duplicates)
- [ ] Refresh page
- [ ] **Expected**: Event still appears only once

### Test 1.2: Multiple Tabs - Create Event
- [ ] Open two tabs to the calendar view
- [ ] In Tab 1: Create a new event
- [ ] In Tab 1: Event should appear immediately
- [ ] In Tab 2: Event should appear via SignalR with notification
- [ ] Check Tab 2 console for: `"Adding event from SignalR"`
- [ ] **Expected**: Event appears once in each tab

### Test 1.3: Update Event Online
- [ ] Update an event (edit or drag/drop)
- [ ] Verify event updates immediately in UI
- [ ] Check console for pendingLocalChanges tracking
- [ ] Wait for SignalR broadcast
- [ ] **Expected**: No duplicate update, no notification for own update
- [ ] In another tab: Should see update notification

### Test 1.4: Delete Event Online
- [ ] Delete an event
- [ ] Verify event removes immediately from UI
- [ ] Wait for SignalR broadcast
- [ ] **Expected**: Event doesn't reappear, no duplicate deletion

## Test Suite 2: Offline Event Creation (Temp ID Mapping)

### Test 2.1: Create Event Offline
- [ ] Go offline: DevTools → Network → Select "Offline"
- [ ] Check connection indicator: Should show offline (red)
- [ ] Create a new event
- [ ] Check console for: `"Event {TempId} created offline"`
- [ ] Verify TempId is negative (e.g., -123456789)
- [ ] **Expected**: Event appears with temporary ID
- [ ] Event should have "(offline - will sync)" message

### Test 2.2: Sync Offline Event
- [ ] Go back online: DevTools → Network → Select "No throttling"
- [ ] Check connection indicator: Should show online (green)
- [ ] Wait for automatic sync (should happen within 2 seconds)
- [ ] Check console for:
   - `"Found X pending operations - triggering automatic sync"`
   - `"Syncing create operation: {EventTitle}"`
   - `"Mapped temporary ID {TempId} to real ID {RealId}"`
   - `"Replacing temporary event {TempId} with real event {RealId}"`
- [ ] **Expected**: Temporary event replaced with real server event
- [ ] **Expected**: Event appears only ONCE (no duplicate)
- [ ] Refresh page
- [ ] **Expected**: Event persists with real ID

### Test 2.3: Create Multiple Events Offline
- [ ] Go offline
- [ ] Create 3-5 events with different titles
- [ ] Verify all events appear with negative IDs
- [ ] Check pending operations count in UI
- [ ] Go back online
- [ ] Wait for automatic sync
- [ ] **Expected**: All events synced successfully
- [ ] **Expected**: All temporary events replaced with real events
- [ ] **Expected**: No duplicates for any event
- [ ] Check console for: `"Synchronization completed: X operations synced successfully"`

## Test Suite 3: Update Operations on Temporary Events

### Test 3.1: Update Temporary Event (Not Yet Synced)
- [ ] Go offline
- [ ] Create a new event (gets temp ID, e.g., -123)
- [ ] Update the same event (change title or time)
- [ ] Check console for: `"Updating temporary event {TempId} - will merge with pending create operation"`
- [ ] Go back online
- [ ] Wait for sync
- [ ] **Expected**: Only ONE create operation sent to server (with merged updates)
- [ ] **Expected**: No separate update operation
- [ ] **Expected**: Event appears with updated values

### Test 3.2: Update Real Event Offline
- [ ] Have an existing event (with positive ID)
- [ ] Go offline
- [ ] Update the existing event
- [ ] Check pending operations count increases
- [ ] Go back online
- [ ] Wait for sync
- [ ] **Expected**: Separate update operation sent to server
- [ ] **Expected**: Event updates successfully

### Test 3.3: Multiple Updates to Temporary Event
- [ ] Go offline
- [ ] Create event A
- [ ] Update event A (change 1)
- [ ] Update event A (change 2)
- [ ] Update event A (change 3)
- [ ] Check pending operations: Should still be just 1 create operation
- [ ] Go back online
- [ ] **Expected**: Single create sent with all changes merged
- [ ] **Expected**: No duplicate or redundant operations

## Test Suite 4: Delete Operations on Temporary Events

### Test 4.1: Delete Temporary Event (Not Yet Synced)
- [ ] Go offline
- [ ] Create a new event (gets temp ID)
- [ ] Delete the same event
- [ ] Check console for: `"Deleting temporary event {TempId} - will remove pending create operation"`
- [ ] Check pending operations count: Should decrease
- [ ] Go back online
- [ ] **Expected**: NO operation sent to server (create was cancelled)
- [ ] **Expected**: Event does not appear after sync

### Test 4.2: Delete Real Event Offline
- [ ] Have an existing event (with positive ID)
- [ ] Go offline
- [ ] Delete the existing event
- [ ] Check pending operations count increases
- [ ] Event should disappear from UI
- [ ] Go back online
- [ ] Wait for sync
- [ ] **Expected**: Delete operation sent to server
- [ ] **Expected**: Event permanently deleted

### Test 4.3: Create, Update, Then Delete Temporary Event
- [ ] Go offline
- [ ] Create event A
- [ ] Update event A
- [ ] Delete event A
- [ ] Check pending operations: Should be 0
- [ ] Go back online
- [ ] **Expected**: NO operations sent (everything cancelled out)

## Test Suite 5: Cache Consistency

### Test 5.1: Verify Cache Updates After Sync
- [ ] Go offline
- [ ] Create events A, B, C
- [ ] Go online and wait for sync
- [ ] Go offline again (without refresh)
- [ ] **Expected**: Events A, B, C should still be visible
- [ ] **Expected**: Events should have real IDs (not temp IDs)
- [ ] Verify in DevTools → Application → IndexedDB → EventSchedulerOfflineDB
- [ ] **Expected**: Only real events in cache, no temporary events

### Test 5.2: Multiple Offline/Online Transitions
- [ ] Go offline → Create event → Go online → Wait for sync
- [ ] Go offline → Create event → Go online → Wait for sync
- [ ] Go offline → Create event → Go online → Wait for sync
- [ ] Refresh page
- [ ] **Expected**: All events visible, no duplicates
- [ ] **Expected**: Clean pending operations queue

### Test 5.3: Cache Cleanup
- [ ] Inspect IndexedDB (DevTools → Application → IndexedDB)
- [ ] Check 'events' store: Should only contain real events (positive IDs)
- [ ] Check 'pendingOperations' store: Should be empty when online and synced
- [ ] **Expected**: No temporary events lingering in cache

## Test Suite 6: Multi-Client Synchronization

### Test 6.1: Two Tabs - Create Event
- [ ] Open Tab 1 and Tab 2
- [ ] In Tab 1: Create event X
- [ ] In Tab 2: Wait for SignalR notification
- [ ] **Expected**: Event X appears in Tab 2 with notification
- [ ] **Expected**: Tab 2 shows "Event 'X' created!" message
- [ ] **Expected**: No duplicate in either tab

### Test 6.2: Two Tabs - One Offline, One Online
- [ ] Tab 1: Go offline
- [ ] Tab 2: Stay online
- [ ] In Tab 1: Create event Y (offline)
- [ ] In Tab 2: Should not see event Y yet
- [ ] In Tab 1: Go back online
- [ ] In Tab 1: Wait for sync
- [ ] In Tab 2: Should receive SignalR notification of event Y
- [ ] **Expected**: Event Y appears in both tabs, no duplicates

### Test 6.3: Different Users (If Testing with Multiple Accounts)
- [ ] User 1: Create public event Z
- [ ] User 2: Should see event Z appear (if viewing public events)
- [ ] **Expected**: Proper real-time synchronization across users

## Test Suite 7: Rapid Offline/Online Transitions

### Test 7.1: Quick Toggle
- [ ] Rapidly toggle offline/online 5 times
- [ ] Create events during transitions
- [ ] **Expected**: No crashes or errors
- [ ] **Expected**: All events eventually synced
- [ ] **Expected**: No duplicate sync operations (check console for "Sync already in progress" messages)

### Test 7.2: Sync Interruption
- [ ] Go offline
- [ ] Create 5 events
- [ ] Go online (sync starts)
- [ ] Immediately go offline again (interrupt sync)
- [ ] Go back online
- [ ] **Expected**: Sync resumes and completes
- [ ] **Expected**: All events eventually synced

## Test Suite 8: Edge Cases

### Test 8.1: Page Refresh During Offline
- [ ] Go offline
- [ ] Create events
- [ ] Refresh page (F5)
- [ ] **Expected**: Events still visible from cache
- [ ] **Expected**: Still offline mode
- [ ] Go online
- [ ] **Expected**: Events sync automatically

### Test 8.2: Long Offline Session
- [ ] Go offline
- [ ] Create 10+ events
- [ ] Update some events
- [ ] Delete some events
- [ ] Stay offline for several minutes
- [ ] Go back online
- [ ] **Expected**: All operations sync in correct order
- [ ] **Expected**: Final state is consistent

### Test 8.3: Network Error During Create
- [ ] Be online
- [ ] Start creating an event
- [ ] Lose network during save
- [ ] **Expected**: Event should be queued for offline sync
- [ ] Restore network
- [ ] **Expected**: Event syncs automatically

## Success Criteria

All tests should pass with:
- ✅ No duplicate events in any scenario
- ✅ Temporary IDs properly mapped to real IDs
- ✅ Seamless offline-to-online transitions
- ✅ Clean cache with no orphaned temporary events
- ✅ Efficient operation merging (no redundant API calls)
- ✅ Consistent state across multiple tabs/clients
- ✅ Proper SignalR notifications (only for others' events)
- ✅ No errors in console during normal operation

## Known Limitations

1. **JWT Expiration**: If offline for >8 hours, token expires and re-authentication is needed
2. **Browser Storage Limits**: IndexedDB typically allows 50MB+, varies by browser
3. **No Conflict Resolution UI**: Last-write-wins strategy for concurrent edits
4. **SignalR Requires Connection**: No real-time updates while offline

## Troubleshooting

### Events Not Syncing
- Check browser console for error messages
- Verify API server is running and reachable
- Check IndexedDB for pending operations
- Verify network status indicator shows online

### Duplicate Events
- Clear browser cache and IndexedDB
- Refresh page
- Check console for optimistic tracking messages

### Temporary Events Not Replacing
- Check console for temp ID mapping messages
- Verify OnTempIdMapped event is firing
- Check that SignalR connection is active

---

**Last Updated**: 2025-10-17  
**Version**: 2.0 (Advanced Synchronization)
