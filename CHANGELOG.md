# Changelog

## [Unreleased] - 2025-10-17

### Added
- **Join Public Events**: Users can now join public events as participants
  - Added `UserId` field to `EventInvitation` entity to link participants to user accounts
  - Joined events now appear in user's calendar alongside their own events
  - Join/Leave buttons in public event modal (requires authentication)
  - Real-time participant count updates via SignalR
  - New API endpoints:
    - `POST /api/events/public/{id}/join` - Join a public event
    - `POST /api/events/public/{id}/leave` - Leave a public event

- **History Tab**: Separate tabs for Active and History events
  - Active tab: Shows Scheduled, InProgress, and Late events
  - History tab: Shows Completed and Cancelled events
  - Improved event organization and filtering

- **Automatic Late Status Management**:
  - New "Late" status added to `EventStatus` enum
  - Events automatically marked as "Late" when end date passes
  - Server-side detection when fetching events
  - Auto-revert to "Scheduled" when rescheduled to future date
  - Status badge styling for Late events

### Changed
- Optimized `GetAllAsync` repository method to use single query instead of multiple queries
- Optimized `GetByDateRangeAsync` to include joined events
- Moved late event detection from client-side to server-side
- Improved late event status update performance (batch updates, no reload)
- Enhanced `UpdateEventAsync` with automatic status management

### Removed
- Cleaned up 13 redundant documentation files:
  - BLAZOR_RECONNECTION_AND_AUTH_PERSISTENCE.md
  - CALENDARVIEW_IMPLEMENTATION_COMPLETE.md
  - CALENDAR_LIST_STYLES_EXTRACTED.md
  - EVENT_MODAL_STYLING_FIXED.md
  - FOLDER_RENAMED_STYLES_TO_CSS.md
  - MODAL_WIDTH_AND_ICON_FIXED.md
  - OLD_CSS_FOLDER_CLEANUP_COMPLETE.md
  - RECONNECTION_IMPLEMENTATION_COMPLETE.md
  - STYLES_MIGRATION_100_COMPLETE.md
  - STYLES_MIGRATION_SUMMARY.md
  - STYLES_ORGANIZATION_COMPLETE.md
  - TOAST_NOTIFICATIONS_FIXED.md
  - TOAST_NOTIFICATION_TROUBLESHOOTING.md

### Database Changes
- Added migration: `20251017015530_AddLateStatusAndUserJoinSupport`
  - Added `UserId` column to `EventInvitations` table (nullable)
  - Added index on `EventInvitations.UserId` for query performance
  - Added foreign key relationship from `EventInvitations` to `Users`

### Technical Details
- All changes maintain backward compatibility
- No styling changes made (as per requirements)
- Performance optimizations implemented based on code review feedback
- Proper logging added for tracking join/leave actions and status updates
