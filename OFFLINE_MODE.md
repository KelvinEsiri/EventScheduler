# Offline Mode

EventScheduler supports full offline functionality, allowing you to continue working when internet connectivity is unavailable.

## Quick Overview

✅ **Work Offline** - Create, edit, and delete events without internet
✅ **Auto-Sync** - Changes automatically sync when connection is restored
✅ **Visual Status** - Connection indicator in bottom-right corner
✅ **No Data Loss** - All offline changes are preserved

## Key Features

### When Online
- Events load from server
- Changes save immediately
- Real-time updates via SignalR

### When Offline
- Events load from cache
- Changes queued for sync
- Full UI functionality maintained

### When Reconnecting
- Automatic synchronization
- Queued changes sent to server
- Fresh data downloaded

## Status Indicator

Located in bottom-right corner:
- 🟢 **Green wifi icon** - Online
- 🔴 **Red wifi-off icon** - Offline
- 🔄 **Blue spinning icon** - Syncing
- **Badge** - Pending operations count

## Documentation

For comprehensive technical details, testing procedures, and troubleshooting:

📖 **[Complete Offline Mode Guide](docs/OFFLINE_MODE_GUIDE.md)**  
Comprehensive documentation covering architecture, usage, testing, and troubleshooting.

📋 **[Offline Testing Guide](OFFLINE_TESTING_GUIDE.md)**  
Step-by-step testing procedures and QA checklist.

## Quick Testing

1. Open browser DevTools (F12)
2. Go to Network tab → Select "Offline"
3. Create/edit/delete events
4. Go back online
5. Watch changes sync automatically

## Browser Support

✅ Chrome/Edge, Firefox, Safari, Mobile browsers  
Requires IndexedDB support (all modern browsers)

---

For technical implementation details, see [docs/OFFLINE_MODE_GUIDE.md](docs/OFFLINE_MODE_GUIDE.md)
