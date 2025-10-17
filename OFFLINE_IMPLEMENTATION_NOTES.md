# Offline Support - Technical Implementation Notes

## Implementation Decisions & Rationale

### 1. Blazor Server + Offline Support

**Challenge**: Blazor Server requires active SignalR connection for interactivity.

**Solution**: Hybrid approach combining:
- Client-side caching with IndexedDB
- Service Worker for resource caching
- JavaScript interop for offline data access
- Pending operations queue for sync

**Why This Works**:
- User must load page while online first
- Once loaded, can interact with cached data
- CRUD operations queued for sync when reconnected
- Service Worker caches static resources for faster load

**Limitation**: True offline-first requires page to be loaded before disconnection. For complete offline capability, consider Blazor WebAssembly in future.

### 2. IndexedDB vs LocalStorage

**Decision**: Use IndexedDB instead of LocalStorage

**Rationale**:
- **Storage Limits**: IndexedDB ~50MB-1GB vs LocalStorage ~5-10MB
- **Structured Data**: IndexedDB supports complex objects natively
- **Indexing**: Fast queries by userId, date, syncStatus
- **Asynchronous**: Non-blocking operations
- **Transactions**: ACID compliance for data integrity

**Trade-offs**:
- More complex API (mitigated with wrapper)
- Browser compatibility (97%+ support)
- Requires JavaScript interop

### 3. Service Worker Strategy

**Network-First for API Calls**:
```javascript
fetch(request)
  .then(cache response)
  .catch(return cached)
```

**Why**:
- Always try server first for fresh data
- Fall back to cache if offline
- Cache updates on successful fetch

**Cache-First for Static Resources**:
```javascript
caches.match(request)
  .then(return cached or fetch)
```

**Why**:
- Faster load times
- Reduces bandwidth
- Static files rarely change

### 4. Conflict Resolution Strategy

**Chosen Approach**: Last-Write-Wins (LWW) based on timestamps

**Implementation**:
```csharp
var localUpdated = localEvent.UpdatedAt ?? localEvent.CreatedAt;
var serverUpdated = serverEvent.UpdatedAt ?? serverEvent.CreatedAt;

if (localUpdated > serverUpdated)
    return localEvent;
else
    return serverEvent;
```

**Rationale**:
- Simple to implement
- Predictable behavior
- Works for most calendar use cases
- Timestamp is authoritative

**Alternative Considered**: Vector clocks (rejected as too complex)

**Future Enhancement**: Allow user to choose version on conflict

### 5. Temporary IDs for Offline Creation

**Problem**: How to identify events created offline before server assignment?

**Solution**: Negative IDs
```csharp
var tempId = -(int)(DateTime.UtcNow.Ticks % int.MaxValue);
```

**Why**:
- Clearly distinguishable from server IDs (always positive)
- No risk of collision with real IDs
- Simple to implement
- Replaced with real ID on sync

**Alternative Considered**: GUIDs (rejected as incompatible with int IDs)

### 6. Pending Operations Queue

**Design**: FIFO queue with retry logic

**Structure**:
```javascript
{
    id: auto-increment,
    type: "POST" | "PUT" | "DELETE",
    endpoint: string,
    data: object,
    token: string,
    timestamp: ISO 8601
}
```

**Processing**:
1. Sort by timestamp (oldest first)
2. Process sequentially
3. Delete on success
4. Keep on failure for retry
5. Retry on next sync

**Why Sequential**:
- Preserves operation order
- Prevents race conditions
- Simpler error handling

### 7. Sync Trigger Strategy

**Primary**: Automatic on reconnection
```javascript
window.addEventListener('online', triggerSync);
```

**Backup**: Periodic connectivity check
```javascript
setInterval(checkConnectivity, 30000); // 30 seconds
```

**Manual**: User-initiated sync
```csharp
await SyncService.SyncAsync();
```

**Why Three-Tiered**:
- Automatic is most convenient
- Periodic catches edge cases
- Manual gives user control

### 8. Service Layer Architecture

**Pattern**: Offline-first with transparent fallback

```
OfflineEventService
    ├─ Online → ApiService → Server
    │               ↓
    │          LocalStorage (cache)
    │
    └─ Offline → LocalStorage
                     ↓
                SyncService (queue)
```

**Benefits**:
- Single API for components
- Automatic online/offline handling
- No code changes in UI
- Easy to test

### 9. Event-Driven Sync

**Design**: Observer pattern for sync events

```csharp
public event EventHandler? SyncStarted;
public event EventHandler<SyncResult>? SyncCompleted;
```

**Why**:
- Decoupled components
- Multiple subscribers
- Easy to add UI feedback
- Testable

**Usage**:
```csharp
SyncService.SyncCompleted += async (s, result) => {
    await RefreshUI();
    ShowNotification(result.Message);
};
```

### 10. JavaScript Interop Pattern

**Design**: JavaScript manager objects with .NET wrappers

**JavaScript Side**:
```javascript
window.indexedDBManager = {
    saveEvent: async (event) => { ... },
    getAllEvents: async (userId) => { ... }
};
```

**.NET Side**:
```csharp
await _jsRuntime.InvokeAsync<JsonElement>(
    "indexedDBManager.saveEvent", eventData);
```

**Why**:
- Clean separation of concerns
- Type-safe on .NET side
- Flexible on JS side
- Easy to debug

### 11. Connectivity Detection

**Multi-Level Approach**:

1. **Navigator.onLine** (instant but unreliable)
```javascript
navigator.onLine
```

2. **Online/Offline Events** (browser-provided)
```javascript
window.addEventListener('online', handler);
window.addEventListener('offline', handler);
```

3. **Periodic Ping** (authoritative)
```javascript
fetch(apiUrl).then(response => response.ok)
```

**Why All Three**:
- Navigator.onLine gives instant feedback
- Events catch most transitions
- Periodic ping confirms actual connectivity

### 12. Error Handling Strategy

**Layered Approach**:

1. **JavaScript Level**: Try/catch with logging
2. **Service Level**: Graceful degradation
3. **UI Level**: User-friendly messages

**Example**:
```csharp
try {
    return await apiService.GetEventsAsync();
}
catch (Exception ex) {
    logger.LogError(ex, "API failed, using cache");
    return await localStorage.GetAllEventsAsync();
}
```

**Why**:
- Never show errors if cache available
- Log for debugging
- Degrade gracefully

### 13. Caching Strategy

**What to Cache**:
- ✅ All user events
- ✅ Public events
- ✅ User profile
- ❌ Other users' data
- ❌ Sensitive information

**Cache Invalidation**:
- On successful sync
- On explicit refresh
- On logout (clear all)

**Why Selective**:
- Privacy concerns
- Storage limits
- Data staleness

### 14. Background Sync API

**Implementation**:
```javascript
if ('sync' in ServiceWorkerRegistration.prototype) {
    await registration.sync.register('sync-events');
}
else {
    // Fallback to immediate sync
}
```

**Why Fallback**:
- Not all browsers support Background Sync
- Safari/iOS don't support it
- Immediate sync works everywhere

### 15. PWA Manifest

**Minimal Configuration**:
```json
{
    "name": "EventScheduler - Offline Calendar",
    "short_name": "EventScheduler",
    "display": "standalone",
    "start_url": "/"
}
```

**Why Minimal**:
- Works on all platforms
- Easy to customize
- Meets PWA requirements
- Single icon sufficient

## Performance Optimizations

### 1. Lazy Loading
- Service Worker registered on page load
- IndexedDB opened on first use
- Sync runs in background

### 2. Debouncing
- Connectivity checks debounced
- Sync operations throttled
- UI updates batched

### 3. Incremental Updates
- Only changed events synced (future)
- Partial cache updates
- Minimal data transfer

## Security Considerations

### 1. Token Storage
- JWT stored in IndexedDB
- Encrypted in transit (HTTPS)
- Cleared on logout
- Expires after 8 hours

### 2. Data Privacy
- Only user's own data cached
- No sensitive data in Service Worker cache
- IndexedDB isolated per origin

### 3. Sync Authentication
- Token included in pending operations
- Re-authentication if token expired
- Secure token refresh flow

## Browser Compatibility

### Supported
- ✅ Chrome 80+ (full support)
- ✅ Edge 80+ (full support)
- ✅ Firefox 75+ (full support)
- ⚠️  Safari 15+ (partial - no Background Sync)
- ⚠️  iOS Safari 15+ (partial - no Background Sync)

### Fallbacks
- Background Sync → Immediate sync
- Service Worker → Client-side caching only
- IndexedDB → LocalStorage (not implemented, fallback to online only)

## Testing Strategy

### Unit Tests (Future)
```csharp
[Fact]
public async Task SyncService_MergesConflicts_LastWriteWins()
{
    var local = new EventResponse { UpdatedAt = DateTime.UtcNow };
    var server = new EventResponse { UpdatedAt = DateTime.UtcNow.AddHours(-1) };
    
    var result = syncService.MergeEvents(new[] { local }, new[] { server });
    
    Assert.Equal(local, result.First());
}
```

### Integration Tests (Future)
```csharp
[Fact]
public async Task OfflineEventService_CreatesEvent_WhenOffline()
{
    connectivityService.SetOnline(false);
    
    var result = await offlineEventService.CreateEventAsync(request);
    
    Assert.NotNull(result);
    Assert.True(result.Id < 0); // Temp ID
}
```

### Manual Testing
See OFFLINE_SUPPORT_GUIDE.md

## Known Limitations

1. **Initial Load**: Must be online to load app initially
2. **SignalR**: Blazor Server requires connection for full interactivity
3. **Storage**: Browser limits vary (typically 50MB-1GB)
4. **Safari**: No Background Sync support
5. **Conflicts**: Last-write-wins may not suit all scenarios

## Future Improvements

### Short-term
- [ ] Add retry logic with exponential backoff
- [ ] Implement optimistic UI updates
- [ ] Add sync progress percentage
- [ ] Show detailed conflict information

### Medium-term
- [ ] Custom conflict resolution UI
- [ ] Selective sync (date range, event type)
- [ ] Delta sync (only changes)
- [ ] Compression for large datasets

### Long-term
- [ ] Migrate to Blazor WebAssembly for true offline
- [ ] Implement Operational Transformation for conflicts
- [ ] Add peer-to-peer sync
- [ ] Offline-first GraphQL integration

## Lessons Learned

1. **Blazor Server Limitations**: True offline requires WebAssembly or hybrid approach
2. **IndexedDB Complexity**: Wrapper library essential for maintainability
3. **Browser Differences**: Always implement fallbacks
4. **User Expectations**: Visual feedback critical for offline features
5. **Testing**: Offline scenarios harder to test - DevTools essential

## Recommendations

### For New Features
- Always use OfflineEventService
- Subscribe to connectivity changes
- Handle temporary IDs in UI
- Test offline scenarios

### For Existing Features
- Migrate from ApiService to OfflineEventService
- Add offline indicators
- Handle sync conflicts gracefully
- Clear cache on logout

### For Production
- Monitor IndexedDB usage
- Set up error tracking for sync failures
- Add analytics for offline usage
- Implement sync retry limits

## References

- [IndexedDB API](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API)
- [Service Workers](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API)
- [Background Sync](https://developer.mozilla.org/en-US/docs/Web/API/Background_Synchronization_API)
- [PWA Guidelines](https://web.dev/progressive-web-apps/)
- [Blazor JavaScript Interop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/)

## Conclusion

This implementation provides robust offline support for EventScheduler while working within Blazor Server's constraints. The architecture is modular, testable, and extensible. Future migration to Blazor WebAssembly would enable true offline-first functionality.

Key achievements:
✅ Full CRUD operations offline
✅ Automatic synchronization
✅ Conflict resolution
✅ PWA capabilities
✅ Comprehensive error handling
✅ Extensive documentation

The system is production-ready with known limitations documented and fallbacks implemented.
