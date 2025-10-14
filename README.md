# Event Scheduler

A modern event scheduling application built with C# .NET 9.0 and Blazor, following Clean Architecture principles.

## Features

- ✅ **User Authentication** - Secure registration and login with JWT tokens
- ✅ **Event Management** - Create, edit, delete, and view events
- ✅ **Calendar View** - Organize events with an intuitive interface
- ✅ **Email Notifications** - Get notified about profile creation, password resets, and event completions
- ✅ **Responsive UI** - Built with Bootstrap 5 for mobile and desktop
- ✅ **Clean Architecture** - Modular, maintainable, and testable codebase
- ✅ **Secure** - Password hashing, JWT authentication, and protected endpoints

## Technology Stack

### Backend
- ASP.NET Core 9.0 Web API
- Entity Framework Core 9.0
- SQLite Database
- JWT Bearer Authentication
- Serilog for structured logging

### Frontend
- Blazor Server (.NET 9.0)
- Bootstrap 5
- SignalR for real-time updates

## Project Structure

```
EventScheduler/
├── EventScheduler.Domain/          # Core business entities
├── EventScheduler.Application/     # Business logic and DTOs
├── EventScheduler.Infrastructure/  # Data access and repositories
├── EventScheduler.Api/            # REST API (Port 5001)
└── EventScheduler.Web/            # Blazor Web App (Port 5070)
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Your favorite IDE (Visual Studio, VS Code, or Rider)

### Running the Application

#### Option 1: Using Helper Scripts

**Linux/Mac:**
```bash
chmod +x run-all.sh
./run-all.sh
```

**Windows:**
```batch
run-all.bat
```

#### Option 2: Manual Start

**Terminal 1 - Start API:**
```bash
cd EventScheduler.Api
dotnet run
```

**Terminal 2 - Start Web:**
```bash
cd EventScheduler.Web
dotnet run
```

Then open your browser to: `http://localhost:5070`

## Usage

1. **Register** - Create a new account at `/register`
2. **Login** - Sign in at `/login`
3. **View Calendar** - See all your events at `/calendar`
4. **Create Event** - Click "New Event" to add events
5. **Manage Events** - Edit or delete events directly from the calendar

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/password-reset-request` - Request password reset
- `POST /api/auth/password-reset` - Reset password

### Events (Protected)
- `GET /api/events` - Get all events
- `GET /api/events/{id}` - Get event by ID
- `GET /api/events/date-range` - Get events by date range
- `POST /api/events` - Create event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event

## Architecture

This project follows **Clean Architecture** principles:

1. **Domain Layer** - Core business entities (User, Event, EventCategory)
2. **Application Layer** - Business logic, DTOs, and service interfaces
3. **Infrastructure Layer** - Data access, repositories, and EF Core
4. **API Layer** - REST API controllers and middleware
5. **Web Layer** - Blazor UI components and pages

### Key Patterns
- Repository Pattern
- Dependency Injection
- Service Layer Pattern
- DTO Pattern
- Middleware Pattern

## Security Features

- ✅ Password hashing with PBKDF2 (10,000 iterations)
- ✅ JWT token authentication
- ✅ Protected API endpoints
- ✅ CORS configuration
- ✅ Input validation
- ✅ Error handling middleware

## Email Notifications

The application sends email notifications for:
- Profile creation (Welcome email)
- Password reset requests
- Event completion

*Note: Currently using logging implementation. Configure SMTP settings for production.*

## Database

- **Development**: SQLite (`eventscheduler.db`)
- **Production**: Can be switched to SQL Server or PostgreSQL

### Schema
- **Users** - User accounts and authentication
- **Events** - Event details and scheduling
- **EventCategories** - Event categorization (optional)

## Configuration

### API (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=eventscheduler.db"
  },
  "Jwt": {
    "Key": "Your-Secret-Key-Here",
    "Issuer": "EventScheduler.Api",
    "Audience": "EventScheduler.Client"
  }
}
```

### Web (appsettings.json)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5001"
  }
}
```

## Development

### Build
```bash
dotnet build
```

### Test
```bash
dotnet test
```

### Database Migrations (if needed)
```bash
cd EventScheduler.Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License.

## Acknowledgments

Built following best practices from the NasosoTax architecture documentation, demonstrating:
- Clean Architecture
- RESTful API design
- Modern Blazor patterns
- Security best practices
- Modular design

## Support

For issues or questions, please open an issue on GitHub.

---

**Built with ❤️ using .NET 9.0 and Blazor**