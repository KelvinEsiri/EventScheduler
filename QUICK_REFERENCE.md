# Event Scheduler - Quick Reference Card

## 🚀 Quick Start (3 Steps)

### 1. Start API
```bash
cd EventScheduler.Api
dotnet run
```
*Wait for: "Starting EventScheduler API on http://localhost:5005"*

### 2. Start Web (New Terminal)
```bash
cd EventScheduler.Web
dotnet run
```
*Wait for: Application started*

### 3. Open Browser
```
http://localhost:5292
```

---

## 📍 URL Reference

| Page | URL | Authentication |
|------|-----|----------------|
| Home | http://localhost:5292/ | Public |
| Register | http://localhost:5292/register | Public |
| Login | http://localhost:5292/login | Public |
| Calendar List | http://localhost:5292/calendar | Required |
| Calendar Grid | http://localhost:5292/calendar-view | Required |
| API Health | http://localhost:5005/ | Public |

---

## 🔑 Default Ports

- **API**: 5005
- **Web**: 5292
- **Database**: LocalDB (no port needed)

---

## 📝 Features Checklist

### ✅ Working Features
- [x] User Registration
- [x] User Login/Logout
- [x] Create Events
- [x] Edit Events
- [x] Delete Events
- [x] Calendar List View
- [x] Calendar Grid View (NEW!)
- [x] All-Day Events
- [x] Event Status
- [x] Event Location
- [x] Responsive Design
- [x] Authentication
- [x] Authorization
- [x] SQL Server Database
- [x] Auto Migrations

### 📧 Email Notifications (Infrastructure Ready)
- [ ] Welcome Email (on registration)
- [ ] Password Reset Email
- [ ] Event Completion Email
*Note: Logging to console for now. Configure SMTP for production.*

---

## 🎨 UI Pages

### Public Pages
1. **Home (/)** - Landing page with features
2. **Register (/register)** - Create account
3. **Login (/login)** - Sign in

### Protected Pages (Login Required)
4. **Calendar List (/calendar)** - Card-based view
5. **Calendar Grid (/calendar-view)** - Monthly calendar
6. **Logout (/logout)** - Sign out

---

## 🔧 Common Commands

### Start Application
```bash
# Option 1: Script
run-all.bat  # Windows
./run-all.sh # Linux/Mac

# Option 2: Manual
# Terminal 1
cd EventScheduler.Api && dotnet run

# Terminal 2
cd EventScheduler.Web && dotnet run
```

### Build & Test
```bash
# Build solution
dotnet build

# Clean build
dotnet clean && dotnet build

# Restore packages
dotnet restore
```

### Database Commands
```bash
# Create migration
dotnet ef migrations add MigrationName --project EventScheduler.Infrastructure --startup-project EventScheduler.Api

# Apply migrations
dotnet ef database update --project EventScheduler.Infrastructure --startup-project EventScheduler.Api

# Drop database
dotnet ef database drop --force --project EventScheduler.Infrastructure --startup-project EventScheduler.Api
```

---

## 🐛 Quick Fixes

### Problem: "Cannot submit form on Register page"
**Solution**: ✅ FIXED - Added `@rendermode InteractiveServer`

### Problem: "API not responding"
**Solution**: Start API first, then Web app

### Problem: "Database error"
**Solution**: 
```bash
cd EventScheduler.Infrastructure
dotnet ef database drop --force --startup-project ../EventScheduler.Api
# Restart API - migrations auto-apply
```

### Problem: "Port already in use"
**Solution**: 
```bash
# Find and kill process
netstat -ano | findstr :5005
taskkill /PID <PID> /F
```

---

## 📊 Architecture Quick View

```
Web (Blazor) → API (REST) → Application (Services) → Infrastructure (EF Core) → Database (SQL Server)
```

### Layers
1. **Web**: Blazor Server UI
2. **API**: REST endpoints  
3. **Application**: Business logic
4. **Infrastructure**: Data access
5. **Domain**: Core entities

---

## 🔐 Security

| Feature | Status |
|---------|--------|
| Password Hashing | ✅ PBKDF2 |
| JWT Tokens | ✅ 8-hour expiration |
| Protected Routes | ✅ [Authorize] |
| SQL Injection Prevention | ✅ EF Core |
| CORS | ✅ Configured |

---

## 📦 Key Files

### Configuration
- `EventScheduler.Api/appsettings.json` - API config
- `EventScheduler.Web/appsettings.json` - Web config
- `EventScheduler.Api/Program.cs` - API startup
- `EventScheduler.Web/Program.cs` - Web startup

### Important Components
- `Components/Layout/MainLayout.razor` - Main layout
- `Components/Pages/Calendar.razor` - List view
- `Components/Pages/CalendarView.razor` - Grid view
- `Services/ApiService.cs` - HTTP client
- `Services/AuthStateProvider.cs` - Auth state

### Backend
- `Controllers/AuthController.cs` - Auth endpoints
- `Controllers/EventsController.cs` - Event endpoints
- `Services/AuthService.cs` - Auth logic
- `Services/EventService.cs` - Event logic

---

## 💡 Tips

### Development
- Use **two terminals**: one for API, one for Web
- API must start first (Web depends on it)
- Hot reload works for both projects
- Check logs in `EventScheduler.Api/logs/`

### Testing
- **Test API first**: http://localhost:5005/
- **Then test Web**: http://localhost:5292/
- Use browser DevTools (F12) for debugging
- Check browser console for JavaScript errors

### Database
- Migrations auto-apply on API start
- Database: `EventSchedulerDb_Dev` (development)
- Location: LocalDB instance
- View in: SQL Server Object Explorer (Visual Studio)

---

## 📱 Responsive Breakpoints

- **Mobile**: < 576px
- **Tablet**: 576px - 992px
- **Desktop**: > 992px

All views are mobile-friendly!

---

## 🎯 Next Steps

### Immediate
1. Start both applications
2. Register a test account
3. Create some events
4. Try calendar and grid views

### Short Term
1. Configure SMTP for real email
2. Add event categories UI
3. Implement event search/filter
4. Add recurring events

### Long Term
1. Mobile app
2. Calendar integrations
3. Team features
4. Advanced reporting

---

## 📚 Documentation

- `README.md` - Main docs
- `COMPLETE_IMPLEMENTATION_GUIDE.md` - Full guide
- `IMPLEMENTATION_SUMMARY.md` - Technical summary
- `docs/` - Detailed documentation

---

## 🆘 Support

### If Something Breaks
1. Stop all running instances (Ctrl+C)
2. Drop database (see command above)
3. Restart API (migrations auto-apply)
4. Restart Web
5. Clear browser cache
6. Try again

### Still Having Issues?
- Check `EventScheduler.Api/logs/` for errors
- Check browser console (F12)
- Verify SQL Server LocalDB is running:
  ```bash
  sqllocaldb info
  sqllocaldb start MSSQLLocalDB
  ```

---

## ✅ Verification Checklist

Before considering done:
- [ ] API starts without errors
- [ ] Web app starts without errors
- [ ] Can register a new user
- [ ] Can login
- [ ] Can create an event
- [ ] Can edit an event
- [ ] Can delete an event
- [ ] Calendar list view works
- [ ] Calendar grid view works
- [ ] Logout works
- [ ] Can login again

---

**Status**: ✅ ALL SYSTEMS GO!

**Last Updated**: October 15, 2025
