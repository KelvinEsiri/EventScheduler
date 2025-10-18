# EventScheduler Documentation Index

## 📋 Quick Navigation

### 🚀 Getting Started
- **[README.md](README.md)** - Project overview and setup instructions
- **[QUICK_START.md](QUICK_START.md)** - Quick start guide
- **[CHANGELOG.md](CHANGELOG.md)** - Version history and changes

### 🏗️ Architecture & Design
- **[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)** - Project folder structure
- **[PROJECT_DESIGN_REFERENCE.md](PROJECT_DESIGN_REFERENCE.md)** - Design patterns and principles
- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** - System architecture overview
- **[SHARED_SERVICES.md](SHARED_SERVICES.md)** - Shared services documentation

### 📡 Offline Mode (⭐ PRIMARY DOCUMENTATION)
- **[OFFLINE_MODE_COMPREHENSIVE_GUIDE.md](OFFLINE_MODE_COMPREHENSIVE_GUIDE.md)** ⭐ - Complete offline implementation guide with architecture, critical components, and testing
- **[OFFLINE_MODE_QUICK_REFERENCE.md](OFFLINE_MODE_QUICK_REFERENCE.md)** ⭐ - Quick reference for developers (DO/DON'T rules, code patterns)
- **[OFFLINE_MODE_TROUBLESHOOTING.md](OFFLINE_MODE_TROUBLESHOOTING.md)** ⭐ - Step-by-step troubleshooting guide with diagnostic scripts

### 📅 Calendar Features
- **[FULLCALENDAR_SETUP.md](FULLCALENDAR_SETUP.md)** - FullCalendar integration setup
- **[CALENDARVIEW_QUICK_REFERENCE.md](CALENDARVIEW_QUICK_REFERENCE.md)** - CalendarView component reference
- **[CALENDARVIEW_OFFLINE_SUMMARY.md](CALENDARVIEW_OFFLINE_SUMMARY.md)** - Offline capabilities summary
- **[CALENDARVIEW_OFFLINE_TESTING.md](CALENDARVIEW_OFFLINE_TESTING.md)** - Offline testing procedures
- **[DRAG_DROP_QUICK_START.md](DRAG_DROP_QUICK_START.md)** - Drag and drop implementation
- **[docs/CALENDAR_SIZE_COMPARISON.md](docs/CALENDAR_SIZE_COMPARISON.md)** - Calendar sizing reference
- **[docs/CALENDAR_TROUBLESHOOTING.md](docs/CALENDAR_TROUBLESHOOTING.md)** - Calendar troubleshooting

### 🎨 UI & Styling
- **[STYLES_QUICK_REFERENCE.md](STYLES_QUICK_REFERENCE.md)** - CSS styles reference
- **[STYLES_VISUAL_MAP.md](STYLES_VISUAL_MAP.md)** - Visual styling guide
- **[docs/BUTTON_LAYOUT_STANDARDIZATION.md](docs/BUTTON_LAYOUT_STANDARDIZATION.md)** - Button layout standards
- **[TOGGLE_ALIGNMENT_FIX.md](TOGGLE_ALIGNMENT_FIX.md)** - Toggle alignment fix

### 👥 User Features
- **[ATTENDEES_FEATURE_SUMMARY.md](ATTENDEES_FEATURE_SUMMARY.md)** - Event attendees feature
- **[CREATED_BY_FEATURE_SUMMARY.md](CREATED_BY_FEATURE_SUMMARY.md)** - Event creator tracking
- **[UI_JOINED_EVENTS_ENHANCEMENT.md](UI_JOINED_EVENTS_ENHANCEMENT.md)** - Joined events UI

### 🔧 Development & Fixes
- **[CODE_BEHIND_REFACTORING.md](CODE_BEHIND_REFACTORING.md)** - Code-behind pattern refactoring
- **[SIGNALR_DOUBLE_EVENT_FIX.md](SIGNALR_DOUBLE_EVENT_FIX.md)** - SignalR duplicate event fix
- **[docs/AUTH_REDIRECT_FIX.md](docs/AUTH_REDIRECT_FIX.md)** - Authentication redirect fix
- **[docs/ROUTE_NAMING_STANDARDIZATION.md](docs/ROUTE_NAMING_STANDARDIZATION.md)** - Route naming conventions

### 🗄️ Database & Infrastructure
- **[docs/DATABASE_SETUP.md](docs/DATABASE_SETUP.md)** - Database setup instructions
- **[docs/LOGGING_GUIDE.md](docs/LOGGING_GUIDE.md)** - Logging configuration

### ✅ Testing
- **[TESTING_CHECKLIST.md](TESTING_CHECKLIST.md)** - General testing checklist

### 🛠️ Scripts
- **[run-all.bat](run-all.bat)** / **[run-all.sh](run-all.sh)** - Start all services
- **[restart-servers.bat](restart-servers.bat)** / **[restart-servers.sh](restart-servers.sh)** - Restart services

---

## 📖 Recommended Reading Order

### For New Developers:
1. **README.md** - Understand the project
2. **QUICK_START.md** - Get it running
3. **PROJECT_STRUCTURE.md** - Learn the codebase layout
4. **OFFLINE_MODE_QUICK_REFERENCE.md** - Understand offline mode rules

### For Offline Mode Work:
1. **OFFLINE_MODE_QUICK_REFERENCE.md** - Critical rules and patterns (read first!)
2. **OFFLINE_MODE_COMPREHENSIVE_GUIDE.md** - Deep dive into architecture
3. **OFFLINE_MODE_TROUBLESHOOTING.md** - When things break

### For Calendar Development:
1. **CALENDARVIEW_QUICK_REFERENCE.md** - Component overview
2. **FULLCALENDAR_SETUP.md** - FullCalendar integration
3. **DRAG_DROP_QUICK_START.md** - Drag/drop implementation

---

## 🎯 Quick Links by Task

| I need to... | Read this |
|--------------|-----------|
| Set up the project | README.md, QUICK_START.md |
| Understand offline mode | OFFLINE_MODE_QUICK_REFERENCE.md |
| Fix offline bugs | OFFLINE_MODE_TROUBLESHOOTING.md |
| Add calendar features | CALENDARVIEW_QUICK_REFERENCE.md |
| Style components | STYLES_QUICK_REFERENCE.md |
| Set up database | docs/DATABASE_SETUP.md |
| Debug issues | OFFLINE_MODE_TROUBLESHOOTING.md, docs/CALENDAR_TROUBLESHOOTING.md |
| Run the app | run-all.bat (Windows) or run-all.sh (Linux/Mac) |

---

## ⚠️ Critical Documents (Never Delete)

These documents contain critical implementation details:

- ✅ **OFFLINE_MODE_COMPREHENSIVE_GUIDE.md** - Contains critical warnings and "never change" sections
- ✅ **OFFLINE_MODE_QUICK_REFERENCE.md** - Contains DO/DON'T rules that prevent bugs
- ✅ **OFFLINE_MODE_TROUBLESHOOTING.md** - Contains diagnostic scripts and recovery procedures
- ✅ **SIGNALR_DOUBLE_EVENT_FIX.md** - Prevents duplicate event handling
- ✅ **CODE_BEHIND_REFACTORING.md** - Documents code organization patterns

---

## 📝 Documentation Standards

### File Naming Conventions:
- `FEATURE_NAME.md` - Feature documentation
- `FEATURE_NAME_SUMMARY.md` - Brief overview
- `FEATURE_NAME_GUIDE.md` - Detailed guide
- `FEATURE_NAME_QUICK_REFERENCE.md` - Quick reference
- `docs/SPECIFIC_TOPIC.md` - Specific technical documentation

### Documentation Types:
- **Guide** - Step-by-step instructions
- **Reference** - Quick lookup information
- **Summary** - High-level overview
- **Troubleshooting** - Problem-solving steps
- **Quick Start** - Fast setup instructions

---

**Last Updated**: October 18, 2025  
**Index Version**: 1.0  
**Total Documents**: 35+
