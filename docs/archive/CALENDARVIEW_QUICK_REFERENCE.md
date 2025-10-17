# Quick Reference - CalendarView Features

## 🎯 Quick Start

### Running the Application
```powershell
# Start API (Terminal 1)
cd EventScheduler.Api
dotnet run

# Start Web (Terminal 2)
cd EventScheduler.Web
dotnet run
```

Navigate to: `http://localhost:5006/calendar-view`

---

## 🎨 Event Type Icons & Colors

| Type | Icon | Color | Use Case |
|------|------|-------|----------|
| Festival | 🎉 | Pink (#ec4899) | Celebrations, parties |
| Interview | 💼 | Purple (#8b5cf6) | Job interviews |
| Birthday | 🎂 | Orange (#f97316) | Birthday celebrations |
| Exam | 📝 | Dark Red (#dc2626) | Tests, exams |
| Appointment | 🏥 | Cyan (#06b6d4) | Doctor, dentist |
| Meeting | 👥 | Blue (#3b82f6) | Business meetings |
| Reminder | ⏰ | Yellow (#eab308) | Reminders, alerts |
| Task | ✅ | Teal (#14b8a6) | To-do items |
| Other | 📅 | Indigo (#6366f1) | Everything else |

---

## 🎨 Status Colors

| Status | Color | Example |
|--------|-------|---------|
| Scheduled | Blue | Default state |
| In Progress | Amber | Active events |
| Completed | Green | Finished events |
| Cancelled | Red | Cancelled events |

---

## ⌨️ User Interactions

### Creating Events
1. **Click "New Event"** - Opens empty form
2. **Click a date** - Pre-fills date (9 AM - 10 AM)
3. **Select date range** - Click and drag across dates

### Viewing Events
1. **Click event** - Opens details modal
2. **Hover event** - Shows tooltip (if description exists)

### Editing Events
1. Click event to open details
2. Click "Edit" button
3. Modify fields
4. Click "Update"

### Deleting Events
1. Click event to open details
2. Click "Delete" button
3. Confirm deletion

### Rescheduling
- **Drag event** to new date/time
- Changes save automatically

### Changing Duration
- **Drag top edge** to change start time
- **Drag bottom edge** to change end time
- Changes save automatically

---

## 🔧 Calendar Views

| View | Description | Best For |
|------|-------------|----------|
| Month | Shows entire month | Overview, planning |
| Week | Time-grid weekly view | Detailed scheduling |
| Day | Single day time-grid | Focus on one day |
| List | Chronological list | Quick scanning |

---

## 🎛️ Form Fields

### Required
- ✅ **Title** (1-200 chars)
- ✅ **Start Date/Time**
- ✅ **End Date/Time**

### Optional
- 📝 **Description** (up to 1000 chars)
- 📍 **Location** (up to 200 chars)
- 🏷️ **Event Type** (dropdown)
- 🌅 **All-Day** (toggle)
- 🌍 **Public** (toggle)
- 👥 **Invitations** (name + email)

---

## 🔔 Real-Time Updates

### Automatic Refresh When:
- ✅ You create an event
- ✅ You update an event
- ✅ You delete an event
- ✅ Another user creates an event
- ✅ Another user updates an event
- ✅ Another user deletes an event

### Connection Status
- 🟢 **Green** - Connected (real-time active)
- 🔴 **Red** - Disconnected (updates delayed)
- 🟡 **Yellow** - Reconnecting (temporary)

---

## 📱 Responsive Breakpoints

| Device | Width | Features |
|--------|-------|----------|
| Desktop | > 768px | Full layout, all features |
| Tablet | 481-768px | Optimized spacing |
| Mobile | < 480px | Icon-only buttons, compact |

---

## 🐛 Troubleshooting

### Calendar Not Loading
1. Check browser console (F12)
2. Verify FullCalendar CDN is accessible
3. Ensure API is running
4. Check authentication token

### Events Not Appearing
1. Verify API returns events
2. Check browser Network tab
3. Ensure correct date format
4. Check authentication

### SignalR Not Working
1. Check connection status indicator
2. Verify WebSocket support
3. Check browser console errors
4. Restart both API and Web

### Drag & Drop Not Working
1. Verify you're the event organizer
2. Check calendar `editable` setting
3. Ensure event is not in the past
4. Refresh page and try again

---

## 🔐 Security Notes

### What You Can Do
- ✅ View all your events
- ✅ View all public events
- ✅ Create events
- ✅ Edit your own events
- ✅ Delete your own events
- ✅ Join public events

### What You Can't Do
- ❌ Edit other users' events
- ❌ Delete other users' events
- ❌ View private events (not invited)

---

## 💡 Pro Tips

### Productivity Hacks
1. **Quick Create** - Click dates for instant event creation
2. **Batch View** - Use Month view for planning
3. **Focus Mode** - Use Day view for execution
4. **Color Coding** - Use event types for visual organization
5. **All-Day Events** - Perfect for holidays, vacations

### Best Practices
1. Add descriptions for context
2. Include location for in-person events
3. Set realistic durations
4. Use appropriate event types
5. Make events public for team visibility

### Keyboard Shortcuts (FullCalendar)
- **←/→** - Navigate prev/next
- **Today** button - Jump to today
- **ESC** - Close modals

---

## 📊 API Endpoints Used

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/events` | Get all events |
| GET | `/api/events/{id}` | Get single event |
| POST | `/api/events` | Create event |
| PUT | `/api/events/{id}` | Update event |
| DELETE | `/api/events/{id}` | Delete event |

---

## 🎓 Learning Resources

### FullCalendar Docs
- Official: https://fullcalendar.io/docs
- Events: https://fullcalendar.io/docs/event-object
- Views: https://fullcalendar.io/docs/views

### Blazor Docs
- JSInterop: https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/
- Forms: https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/

### SignalR Docs
- Overview: https://learn.microsoft.com/en-us/aspnet/core/signalr/

---

## 🆘 Common Questions

**Q: Can I drag events across months?**
A: Yes! Drag to any visible date in any view.

**Q: How do I make an event recurring?**
A: Not yet implemented. Coming in future update.

**Q: Can I export my calendar?**
A: Not yet implemented. Coming in future update.

**Q: How many invitations can I add?**
A: Unlimited! Click "Add Invitation" multiple times.

**Q: Can I change event color manually?**
A: Currently auto-colored by status/type. Manual override coming soon.

**Q: Do events sync across devices?**
A: Yes! All events stored in database and sync via SignalR.

---

**Last Updated:** October 15, 2025
