# EventScheduler

A modern event scheduling application built with .NET 9.0 and Blazor Server, following Clean Architecture principles.

## Features

- ✅ **User Authentication** - Secure registration and login with JWT tokens
- ✅ **Event Management** - Create, edit, delete, and view events
- ✅ **Calendar Views** - List and grid calendar views
- ✅ **Event Status** - Track events (Scheduled, InProgress, Completed, Cancelled)
- ✅ **Responsive UI** - Built with Bootstrap 5 for mobile and desktop
- ✅ **Clean Architecture** - Modular, maintainable, and testable codebase
- ✅ **Secure** - Password hashing, JWT authentication, and protected endpoints

## Technology Stack

- **Backend**: ASP.NET Core 9.0 Web API
- **Frontend**: Blazor Server (.NET 9.0)
- **Database**: SQL Server (LocalDB for development)
- **ORM**: Entity Framework Core 9.0
- **Authentication**: JWT Bearer tokens
- **Logging**: Serilog
- **UI**: Bootstrap 5

## Project Structure

```
EventScheduler/
├── EventScheduler.Domain/          # Core entities (User, Event, EventCategory)
├── EventScheduler.Application/     # Business logic, services, DTOs
├── EventScheduler.Infrastructure/  # Data access, repositories, EF Core
├── EventScheduler.Api/            # REST API (Port 5005)
└── EventScheduler.Web/            # Blazor Server UI (Port 5292)
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- SQL Server LocalDB (included with Visual Studio)

### Quick Start

#### Option 1: Using Scripts

**Windows:**
```powershell
.\run-all.bat
```

**Linux/Mac:**
```bash
chmod +x run-all.sh && ./run-all.sh
```

#### Option 2: Manual Start

**Terminal 1 - Start API:**
```bash
cd EventScheduler.Api
dotnet run
```
*Wait for: "Starting EventScheduler API on http://localhost:5005"*

**Terminal 2 - Start Web:**
```bash
cd EventScheduler.Web
dotnet run
```
*Wait for: "Application started"*

**Open Browser:**
```
http://localhost:5292
```

## Usage

1. **Register** - Create an account at `/register`
2. **Login** - Sign in at `/login`
3. **Calendar List** - View events at `/calendar`
4. **Calendar Grid** - View monthly calendar at `/calendar-view`
5. **Create Event** - Click "New Event" or click a date on the calendar
6. **Manage Events** - Edit or delete events from either view

## API Endpoints

### Authentication (Public)
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/password-reset-request` - Request password reset
- `POST /api/auth/password-reset` - Reset password with token

### Events (Protected - Requires JWT)
- `GET /api/events` - Get all user events
- `GET /api/events/{id}` - Get specific event
- `GET /api/events/date-range?start=...&end=...` - Get events in date range
- `POST /api/events` - Create new event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event

## Architecture

This project implements **Clean Architecture** with clear separation of concerns:

### Layers
1. **Domain** - Entities and business rules (no dependencies)
2. **Application** - Business logic, services, and interfaces
3. **Infrastructure** - Data access, EF Core, repositories
4. **API** - REST endpoints, JWT auth, middleware
5. **Web** - Blazor UI, pages, components

### Design Patterns
- **Repository Pattern** - Data access abstraction
- **Service Layer** - Business logic encapsulation
- **Dependency Injection** - IoC container
- **DTO Pattern** - Data transfer objects
- **Middleware** - Cross-cutting concerns

## Security

- **Password Hashing** - PBKDF2 with 10,000 iterations
- **JWT Authentication** - Bearer tokens with 8-hour expiration
- **Protected Endpoints** - Authorization on API controllers
- **CORS** - Configured for Web app origin
- **SQL Injection Prevention** - Parameterized queries via EF Core
- **Error Handling** - Global middleware with safe error messages

## Database

- **DBMS**: SQL Server (LocalDB for development)
- **ORM**: Entity Framework Core 9.0
- **Migrations**: Code-first with automatic application on startup

### Tables
- **Users** - Authentication and user profiles
- **Events** - Event details with scheduling
- **EventCategories** - Optional event categorization

See [docs/DATABASE_SETUP.md](docs/DATABASE_SETUP.md) for detailed information.

## Configuration

### API Configuration
**File**: `EventScheduler.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EventSchedulerDb;..."
  },
  "Jwt": {
    "Key": "Your-Secret-Key",
    "Issuer": "EventScheduler.Api",
    "Audience": "EventScheduler.Client"
  }
}
```

### Web Configuration
**File**: `EventScheduler.Web/appsettings.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5005"
  }
}
```

## Development

### Build Solution
```bash
dotnet build
```

### Database Migrations
```bash
# Create migration
cd EventScheduler.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../EventScheduler.Api

# Apply migrations (or just run the API - migrations auto-apply)
dotnet ef database update --startup-project ../EventScheduler.Api

# Drop database
dotnet ef database drop --force --startup-project ../EventScheduler.Api
```

## Documentation

- [Architecture](docs/ARCHITECTURE.md) - System architecture and design patterns
- [Database Setup](docs/DATABASE_SETUP.md) - Database configuration and migrations

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.

---

**Built with .NET 9.0, Blazor Server, and Clean Architecture principles**