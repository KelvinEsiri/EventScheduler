# Event Scheduler - Quick Start Guide

## Prerequisites
- .NET 9.0 SDK installed
- Terminal/Command Prompt

## Installation & Running

### Option 1: One-Command Start (Recommended)

**Linux/macOS:**
```bash
chmod +x run-all.sh && ./run-all.sh
```

**Windows:**
```cmd
run-all.bat
```

### Option 2: Manual Start

**Step 1: Start the API (Terminal 1)**
```bash
cd EventScheduler.Api
dotnet run
```
Wait for: `Now listening on: http://localhost:5001`

**Step 2: Start the Web App (Terminal 2)**
```bash
cd EventScheduler.Web
dotnet run
```
Wait for: `Now listening on: http://localhost:5070`

**Step 3: Open Browser**
Navigate to: **http://localhost:5070**

---

## First-Time Usage

### 1. Register a New Account
- Click **"Get Started"** or **"Register"**
- Fill in:
  - Full Name
  - Username
  - Email
  - Password (min 8 characters)
- Click **"Register"**

### 2. You're Automatically Logged In!
After registration, you'll be redirected to the Calendar page.

### 3. Create Your First Event
- Click **"New Event"** button
- Enter event details:
  - Title (required)
  - Description
  - Start Date & Time
  - End Date & Time
  - Location
  - All Day Event (checkbox)
- Click **"Save"**

### 4. Manage Events
- **Edit**: Click "Edit" button on any event card
- **Delete**: Click "Delete" button on any event card
- **View**: All events are displayed in card format

---

## Default Ports
- **API:** http://localhost:5001
- **Web:** http://localhost:5070

## Database
- Location: `EventScheduler.Api/eventscheduler.db`
- Type: SQLite
- Auto-created on first run

## Logs
- Location: `EventScheduler.Api/logs/`
- Format: `eventscheduler-YYYYMMDD.log`

---

## Testing the API (Optional)

### Register a User
```bash
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test123!",
    "fullName": "Test User"
  }'
```

### Login
```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "Test123!"
  }'
```

### Create Event (use token from login response)
```bash
curl -X POST http://localhost:5001/api/events \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "title": "Team Meeting",
    "description": "Monthly sync",
    "startDate": "2025-10-15T10:00:00",
    "endDate": "2025-10-15T11:00:00",
    "location": "Conference Room",
    "isAllDay": false
  }'
```

---

## Troubleshooting

### Port Already in Use
If you get a port conflict error:

**API (change from 5001):**
```bash
cd EventScheduler.Api
dotnet run --urls="http://localhost:5002"
```

**Web (change from 5070):**
```bash
cd EventScheduler.Web
dotnet run --urls="http://localhost:5071"
```
*Remember to update Web's appsettings.json ApiBaseUrl if you change API port*

### Build Errors
```bash
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

### Database Issues
Delete the database to start fresh:
```bash
rm EventScheduler.Api/eventscheduler.db
```
It will be recreated on next API start.

---

## Project Structure (for developers)

```
EventScheduler/
├── EventScheduler.Domain/          # Business entities
├── EventScheduler.Application/     # Business logic
├── EventScheduler.Infrastructure/  # Data access
├── EventScheduler.Api/            # REST API
└── EventScheduler.Web/            # Blazor UI
```

---

## Features at a Glance

✅ User Registration & Login  
✅ JWT Authentication  
✅ Event CRUD Operations  
✅ Calendar View  
✅ Event Status Tracking  
✅ Email Notifications (logged)  
✅ Responsive Design  
✅ Secure Password Storage  

---

## Support

For issues or questions:
1. Check `README.md` for detailed documentation
2. Check `IMPLEMENTATION_SUMMARY.md` for technical details
3. Open an issue on GitHub

---

## Next Steps

After getting the app running:
1. Explore the Calendar page
2. Create multiple events
3. Try editing and deleting events
4. Check the API logs in the logs folder
5. Review the code structure

---

**Enjoy using Event Scheduler! 🎉**
