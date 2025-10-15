# EventScheduler - Architecture Documentation# NasosoTax - Architecture Documentation



## Overview**Project:** NasosoTax - Tax Management Portal  

**Last Updated:** October 2025  

EventScheduler is a full-stack event management application built with .NET 9.0, following **Clean Architecture** principles. The application separates concerns into distinct layers, ensuring maintainability, testability, and scalability.**Architecture Style:** Clean Architecture with Separation of Concerns



## Architecture Pattern---



**Clean Architecture (Onion Architecture)** with the following layers:## Table of Contents



```1. [Overview](#overview)

┌─────────────────────────────────────────────────┐2. [Architecture Principles](#architecture-principles)

│  Presentation Layer                             │3. [Project Structure](#project-structure)

│  - EventScheduler.Web (Blazor Server)           │4. [Layer Details](#layer-details)

│  - EventScheduler.Api (REST API)                │5. [Communication Flow](#communication-flow)

└──────────────┬──────────────────────────────────┘6. [Deployment Architecture](#deployment-architecture)

               │7. [Architecture Evolution](#architecture-evolution)

┌──────────────▼──────────────────────────────────┐

│  Application Layer                              │---

│  - EventScheduler.Application                   │

│  - Business Logic, Services, DTOs               │## Overview

└──────────────┬──────────────────────────────────┘

               │NasosoTax is built using **Clean Architecture** principles with a clear separation between frontend and backend concerns. The application consists of five main projects organized into distinct layers.

┌──────────────▼──────────────────────────────────┐

│  Infrastructure Layer                           │### Architecture Rating: ⭐⭐⭐⭐⭐ (5/5)

│  - EventScheduler.Infrastructure                │

│  - Data Access, Repositories, EF Core           │**Strengths:**

└──────────────┬──────────────────────────────────┘- ✅ Perfect implementation of Clean Architecture

               │- ✅ Clear separation of concerns

┌──────────────▼──────────────────────────────────┐- ✅ Proper dependency direction

│  Domain Layer                                   │- ✅ Frontend and backend completely separated

│  - EventScheduler.Domain                        │- ✅ Easy to test, scale, and maintain

│  - Core Entities, Business Rules                │- ✅ Industry best practices

└─────────────────────────────────────────────────┘

```---



## Project Structure## Architecture Principles



### 1. EventScheduler.Domain### 1. Separation of Concerns

**Core business entities** - No external dependenciesEach layer has a single, well-defined responsibility:

- **Domain**: Business entities and models

- **Entities/**- **Application**: Business logic and interfaces

  - `User.cs` - User account entity- **Infrastructure**: Data access and external services

  - `Event.cs` - Event entity with scheduling details- **API**: Backend web services

  - `EventCategory.cs` - Event categorization- **Web**: Frontend user interface

- **Enums**

  - `EventStatus` - Event lifecycle states (Scheduled, InProgress, Completed, Cancelled)### 2. Dependency Inversion

Dependencies flow inward toward the domain:

### 2. EventScheduler.Application```

**Business logic and service interfaces**Web/Api → Application → Domain

              ↑

- **Services/** - Service implementations      Infrastructure

  - `AuthService.cs` - Authentication logic```

  - `EventService.cs` - Event management logic

  - `EmailService.cs` - Email notification logic### 3. Technology Independence

  - Core business logic is independent of frameworks

- **Interfaces/**- Can swap UI, database, or external services easily

  - `IAuthService` - Authentication interface- Business rules remain stable

  - `IEventService` - Event management interface

  - `IEmailService` - Email service interface### 4. Testability

  - `IUserRepository` - User data access interface- Each layer can be tested independently

  - `IEventRepository` - Event data access interface- Easy to mock dependencies

- Clear boundaries for unit and integration tests

- **DTOs/** - Data Transfer Objects

  - Request DTOs (LoginRequest, RegisterRequest, CreateEventRequest, etc.)---

  - Response DTOs (LoginResponse, EventResponse, etc.)

## Project Structure

### 3. EventScheduler.Infrastructure

**Data access and external services**```

NasosoTax/

- **Data/**├── NasosoTax.Domain/          # Core business entities and models

  - `EventSchedulerDbContext.cs` - EF Core database context│   ├── Entities/              # Domain entities

  │   │   ├── User.cs

- **Repositories/**│   │   ├── TaxRecord.cs

  - `UserRepository.cs` - User data access implementation│   │   ├── IncomeSource.cs

  - `EventRepository.cs` - Event data access implementation│   │   ├── Deduction.cs

  │   │   ├── GeneralLedger.cs

- **Migrations/** - Entity Framework database migrations│   │   └── MonthlyIncome.cs

│   └── Models/                # Domain models

### 4. EventScheduler.Api│       ├── TaxBracket.cs

**REST API backend** (Port 5005)│       └── TaxCalculationResult.cs

│

- **Controllers/**├── NasosoTax.Application/     # Business logic and contracts

  - `AuthController.cs` - Authentication endpoints│   ├── DTOs/                  # Data Transfer Objects

  - `EventsController.cs` - Event CRUD endpoints│   │   ├── Request/           # API request models

  │   │   └── Response/          # API response models

- **Middleware/**│   ├── Interfaces/            # Service and repository contracts

  - `ErrorHandlingMiddleware.cs` - Global error handling│   │   ├── Services/

  │   │   └── Repositories/

- **Configuration**│   └── Services/              # Business logic implementations

  - JWT authentication│       ├── TaxCalculationService.cs

  - SQL Server database│       ├── AuthService.cs

  - Serilog logging│       ├── ReportsService.cs

  - CORS policy│       └── ValidationHelper.cs

│

### 5. EventScheduler.Web├── NasosoTax.Infrastructure/  # External concerns and data access

**Blazor Server frontend** (Port 5292)│   ├── Data/                  # Database context

│   │   └── TaxDbContext.cs

- **Components/Pages/** - Page components│   ├── Repositories/          # Repository implementations

  - `Home.razor` - Landing page│   │   ├── UserRepository.cs

  - `Register.razor` - User registration│   │   ├── TaxRecordRepository.cs

  - `Login.razor` - User login│   │   └── GeneralLedgerRepository.cs

  - `Calendar.razor` - Event list view│   └── Migrations/            # EF Core migrations

  - `CalendarView.razor` - Calendar grid view│

  - `Logout.razor` - Logout handler├── NasosoTax.Api/             # Backend REST API (Port 5001)

  │   ├── Controllers/           # API endpoints

- **Services/**│   │   ├── AuthController.cs

  - `ApiService.cs` - HTTP client for API communication│   │   ├── TaxController.cs

  - `AuthStateProvider.cs` - Authentication state management│   │   ├── LedgerController.cs

│   │   ├── ReportsController.cs

## Key Design Patterns│   │   └── HealthController.cs

│   ├── Middleware/            # Request pipeline middleware

### Repository Pattern│   │   └── ErrorHandlingMiddleware.cs

Abstracts data access logic from business logic:│   ├── appsettings.json       # API configuration

```csharp│   └── Program.cs             # API startup

public interface IEventRepository│

{└── NasosoTax.Web/             # Frontend Blazor App (Port 5070)

    Task<Event> GetByIdAsync(int id);    ├── Components/            # Blazor components

    Task<IEnumerable<Event>> GetAllByUserIdAsync(int userId);    │   ├── Pages/             # Page components

    Task AddAsync(Event entity);    │   │   ├── Home.razor

    Task UpdateAsync(Event entity);    │   │   ├── Calculator.razor

    Task DeleteAsync(int id);    │   │   ├── Login.razor

}    │   │   ├── Register.razor

```    │   │   ├── SubmitIncome.razor

    │   │   ├── Reports.razor

### Service Layer Pattern    │   │   └── Ledger.razor

Encapsulates business logic:    │   └── Layout/            # Layout components

```csharp    │       ├── MainLayout.razor

public interface IEventService    │       └── NavMenu.razor

{    ├── Services/              # Frontend services

    Task<EventResponse> CreateEventAsync(CreateEventRequest request, int userId);    │   ├── ApiService.cs      # HTTP client wrapper

    Task<IEnumerable<EventResponse>> GetUserEventsAsync(int userId);    │   ├── AuthStateProvider.cs

    Task<bool> UpdateEventAsync(int eventId, UpdateEventRequest request, int userId);    │   └── AuthStateCache.cs

    Task<bool> DeleteEventAsync(int eventId, int userId);    ├── appsettings.json       # Web configuration

}    └── Program.cs             # Web startup

``````



### Dependency Injection---

All dependencies registered in Program.cs and resolved at runtime.

## Layer Details

## Data Flow

### 1. Domain Layer (NasosoTax.Domain) ⭐⭐⭐⭐⭐

### Authentication Flow

1. User submits credentials (Web)**Purpose:** Core business entities and models  

2. Request sent to API `/api/auth/login`**Dependencies:** None (pure domain logic)  

3. AuthController calls AuthService**Rating:** Excellent

4. AuthService validates via UserRepository

5. JWT token generated and returned**Key Components:**

6. Token stored in AuthStateProvider

7. Token included in subsequent API requests#### Entities

- **User**: User accounts with authentication

### Event Management Flow- **TaxRecord**: Tax filing records per year

1. User creates event (Web)- **IncomeSource**: Income sources with monthly breakdowns

2. Request sent to API `/api/events` with JWT token- **Deduction**: Tax deductions (NHF, NHIS, Pension, etc.)

3. JWT validated by middleware- **GeneralLedger**: Financial transaction entries

4. EventsController calls EventService- **MonthlyIncome**: Variable monthly income tracking

5. EventService validates and processes via EventRepository

6. Changes persisted to database#### Models

7. Response returned to Web- **TaxBracket**: Progressive tax bracket definitions

8. UI updated- **TaxCalculationResult**: Tax calculation results with breakdowns



## Database Schema**Strengths:**

- ✅ No external dependencies

### Users Table- ✅ Clear entity relationships

- Id (PK)- ✅ Well-designed navigation properties

- Username (Unique)- ✅ Proper audit fields (CreatedAt, UpdatedAt)

- Email (Unique)

- PasswordHash---

- FullName

- CreatedAt### 2. Application Layer (NasosoTax.Application) ⭐⭐⭐⭐⭐

- LastLoginAt

- EmailVerified**Purpose:** Business logic and service contracts  

- PasswordResetToken**Dependencies:** Domain layer only  

- PasswordResetTokenExpiry**Rating:** Excellent



### Events Table**Key Components:**

- Id (PK)

- Title#### Services

- Description- **TaxCalculationService**: Tax computation based on Nigeria Tax Act 2025

- StartDate- **AuthService**: Authentication and user management

- EndDate- **ReportsService**: Tax report generation

- Location- **ValidationHelper**: Input validation logic

- IsAllDay

- Status (Enum)#### Interfaces

- Color- **IUserRepository**: User data access contract

- UserId (FK → Users)- **ITaxRecordRepository**: Tax record data access contract

- CategoryId (FK → EventCategories, nullable)- **IGeneralLedgerRepository**: Ledger data access contract

- CreatedAt

- UpdatedAt#### DTOs

- **Request DTOs**: LoginRequest, RegisterRequest, TaxSubmissionRequest

### EventCategories Table- **Response DTOs**: TaxCalculationResponse, UserReportResponse

- Id (PK)

- Name**Strengths:**

- Description- ✅ Pure business logic

- Color- ✅ Framework-independent

- UserId (FK → Users)- ✅ Testable in isolation

- CreatedAt- ✅ Clear service boundaries



## Security Features---



1. **Password Hashing** - PBKDF2 with 10,000 iterations### 3. Infrastructure Layer (NasosoTax.Infrastructure) ⭐⭐⭐⭐

2. **JWT Authentication** - Token-based stateless auth (8-hour expiration)

3. **Protected Endpoints** - `[Authorize]` attribute on controllers**Purpose:** Data access and external service implementations  

4. **CORS Policy** - Configured for Web app origin**Dependencies:** Domain, Application layers  

5. **SQL Injection Prevention** - Parameterized queries via EF Core**Rating:** Very Good

6. **Error Handling** - Global middleware with safe error messages

**Key Components:**

## Technology Stack

#### Data Context

- **.NET 9.0** - Latest framework- **TaxDbContext**: Entity Framework Core DbContext

- **Entity Framework Core 9.0** - ORM for data access  - SQLite database

- **SQL Server LocalDB** - Development database  - Entity configurations

- **Blazor Server** - Interactive web UI  - Seed data

- **JWT Bearer** - Authentication tokens  - Migration support

- **Serilog** - Structured logging

- **Bootstrap 5** - UI framework#### Repositories

- **UserRepository**: User CRUD operations

## Configuration- **TaxRecordRepository**: Tax record operations with relationships

- **GeneralLedgerRepository**: Ledger entry operations

### API (appsettings.json)

```json**Strengths:**

{- ✅ Proper repository pattern

  "ConnectionStrings": {- ✅ Entity Framework Core best practices

    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EventSchedulerDb;..."- ✅ Migration support

  },- ✅ Good query optimization

  "Jwt": {

    "Key": "Your-Secret-Key",**Recommendations:**

    "Issuer": "EventScheduler.Api",- ⚠️ Add database indexes for performance

    "Audience": "EventScheduler.Client"- ⚠️ Consider soft deletes for audit trails

  }- ⚠️ Add more database constraints

}

```---



### Web (appsettings.json)### 4. API Layer (NasosoTax.Api) ⭐⭐⭐⭐⭐

```json

{**Purpose:** RESTful backend web API  

  "ApiSettings": {**Dependencies:** Application, Infrastructure layers  

    "BaseUrl": "http://localhost:5005"**Port:** 5001  

  }**Rating:** Excellent

}

```**Key Components:**



## Running the Application#### Controllers

- **AuthController**: User authentication and registration

### Development- **TaxController**: Tax calculations and submissions

1. Start API: `cd EventScheduler.Api && dotnet run`- **LedgerController**: General ledger operations

2. Start Web: `cd EventScheduler.Web && dotnet run`- **ReportsController**: Tax report generation

3. Access: http://localhost:5292- **HealthController**: Health check endpoints



### Production Considerations#### Middleware

- Use environment variables for sensitive configuration- **ErrorHandlingMiddleware**: Global exception handling

- Enable HTTPS only

- Configure production database connection#### Features

- Implement rate limiting- ✅ JWT Bearer authentication

- Add caching layer- ✅ Comprehensive API endpoints

- Configure monitoring and logging- ✅ Proper HTTP status codes

- Set up health checks- ✅ Input validation

- ✅ Structured logging (Serilog)

## Best Practices Implemented- ✅ CORS configuration

- ✅ Error handling

✅ Clean Architecture with clear layer separation  - ✅ Health checks

✅ Dependency Injection throughout  

✅ Repository pattern for data access  **API Design:**

✅ Service layer for business logic  - RESTful conventions

✅ DTO pattern for data transfer  - Consistent response formats

✅ Middleware for cross-cutting concerns  - Proper use of HTTP verbs

✅ JWT for stateless authentication  - Bearer token authentication

✅ Secure password storage  

✅ Structured logging  ---

✅ Error handling middleware  

✅ Database migrations  ### 5. Web Layer (NasosoTax.Web) ⭐⭐⭐⭐

✅ Responsive UI design  

**Purpose:** Frontend user interface  

## Future Enhancements**Dependencies:** Application layer (for DTOs only)  

**Port:** 5070  

- Add unit and integration tests**Rating:** Very Good

- Implement caching (Redis)

- Add API versioning**Key Components:**

- Implement SignalR for real-time updates

- Add health checks#### Pages

- Implement API rate limiting- **Home**: Landing page

- Add Docker support- **Calculator**: Real-time tax calculation

- Implement CI/CD pipeline- **Login/Register**: User authentication

- **SubmitIncome**: Income and deduction submission
- **Reports**: Tax reports and summaries
- **Ledger**: General ledger with filtering

#### Services
- **ApiService**: HTTP client wrapper for API calls
- **AuthStateProvider**: Authentication state management
- **AuthStateCache**: Session-scoped auth caching

#### Features
- ✅ Blazor Server with SignalR
- ✅ Bootstrap 5 UI
- ✅ Responsive design
- ✅ Real-time calculations
- ✅ Form validation
- ✅ Loading states
- ✅ Error handling
- ✅ Session management

**Strengths:**
- ✅ Clean component structure
- ✅ Proper API communication
- ✅ Good UX patterns

**Recommendations:**
- ⚠️ Add loading skeletons
- ⚠️ Implement client-side caching
- ⚠️ Add more reusable components

---

## Communication Flow

### Request Flow
```
┌─────────────────┐       HTTP/REST      ┌─────────────────┐
│                 │──────────────────────▶│                 │
│ NasosoTax.Web   │                      │ NasosoTax.Api   │
│ (Frontend)      │◀──────────────────────│ (Backend)       │
│                 │   JSON Responses      │                 │
│ Port: 5070      │                      │ Port: 5001      │
└─────────────────┘                      └─────────────────┘
     │                                          │
     ▼                                          ▼
┌─────────────────┐                      ┌─────────────────┐
│ Blazor Pages    │                      │  Controllers    │
│ API Service     │                      │  Services       │
│ Auth State      │                      │  Repositories   │
│ HTTP Client     │                      │  Database       │
└─────────────────┘                      └─────────────────┘
```

### Example: Tax Calculation Flow

```
User Input (Calculator.razor)
         │
         ▼
ApiService.CalculateTaxAsync()
         │
         ▼
HTTP POST /api/tax/calculate
         │
         ▼
TaxController.CalculateTax()
         │
         ▼
TaxCalculationService.CalculateTax()
         │
         ▼
Tax Computation Logic
         │
         ▼
Return TaxCalculationResult
         │
         ▼
JSON Response
         │
         ▼
Update UI (Calculator.razor)
```

### Authentication Flow

```
User Login (Login.razor)
         │
         ▼
ApiService.LoginAsync()
         │
         ▼
HTTP POST /api/auth/login
         │
         ▼
AuthController.Login()
         │
         ▼
AuthService.AuthenticateAsync()
         │
         ▼
JWT Token Generation
         │
         ▼
Return Token + User Info
         │
         ▼
Store in AuthStateProvider
         │
         ▼
Redirect to Home
```

---

## Deployment Architecture

### Development Environment
```
Developer Machine
├── Backend API (localhost:5001)
│   ├── Controllers
│   ├── Services
│   └── SQLite Database (nasosotax.db)
└── Frontend Web (localhost:5070)
    ├── Blazor Server
    └── SignalR Hub
```

### Production Options

#### Option 1: Single Server
```
Server (IIS/Kestrel)
├── Backend API (api.nasosotax.com)
└── Frontend Web (www.nasosotax.com)
```

#### Option 2: Separate Servers
```
API Server (api.nasosotax.com)
└── NasosoTax.Api

Web Server (www.nasosotax.com)
└── NasosoTax.Web
```

#### Option 3: Cloud Native (Recommended)
```
Azure/AWS
├── App Service (API)
│   └── NasosoTax.Api
├── App Service (Web)
│   └── NasosoTax.Web
├── SQL Database (Production)
└── Application Insights (Monitoring)
```

#### Option 4: Containerized
```
Docker Compose / Kubernetes
├── nasos otax-api:latest
├── nasosotax-web:latest
└── nasosotax-db:latest
```

---

## Architecture Evolution

### Phase 1: Original Monolith (Problem)
```
NasosoTax.Web (Single Project)
├── Controllers ❌ Mixed with UI
├── Components
├── Services
└── Middleware
```

**Issues:**
- ❌ Controllers mixed with UI
- ❌ Poor separation of concerns
- ❌ Hard to test independently
- ❌ Difficult to scale
- ❌ Team conflicts

### Phase 2: Separated Architecture (Current)
```
NasosoTax/
├── NasosoTax.Domain
├── NasosoTax.Application
├── NasosoTax.Infrastructure
├── NasosoTax.Api ✅ Separated
└── NasosoTax.Web ✅ Separated
```

**Benefits:**
- ✅ Clear separation of concerns
- ✅ Easy to test independently
- ✅ Can scale separately
- ✅ Team independence
- ✅ Reusable API
- ✅ Production ready

### Phase 3: Future Enhancements (Roadmap)

#### Short Term
- Add comprehensive unit tests
- Implement API versioning
- Add Swagger/OpenAPI documentation
- Set up CI/CD pipelines

#### Medium Term
- Add Redis caching layer
- Implement API rate limiting
- Create mobile app using same API
- Add real-time notifications

#### Long Term
- Consider microservices for scaling
- Add event sourcing for audit
- Implement CQRS pattern
- Add machine learning for tax predictions

---

## Architecture Best Practices

### ✅ Current Implementation

1. **Clean Architecture**
   - Clear layer boundaries
   - Proper dependency direction
   - Technology independence

2. **RESTful API Design**
   - Resource-based URLs
   - Proper HTTP verbs
   - Consistent response formats

3. **Security**
   - JWT authentication
   - Authorization on endpoints
   - Input validation
   - SQL injection protection

4. **Logging**
   - Structured logging (Serilog)
   - Log levels (Info, Warning, Error)
   - Contextual information

5. **Error Handling**
   - Global exception middleware
   - Consistent error responses
   - Proper HTTP status codes

### 🔧 Recommended Improvements

1. **Testing**
   - Add unit tests for services
   - Add integration tests for API
   - Add E2E tests for UI

2. **Performance**
   - Add database indexes
   - Implement response caching
   - Optimize queries
   - Add connection pooling

3. **Monitoring**
   - Add health check dashboard
   - Implement APM (Application Performance Monitoring)
   - Add metrics collection
   - Set up alerting

4. **Documentation**
   - Add XML comments to APIs
   - Generate Swagger documentation
   - Add architecture decision records (ADRs)

5. **Security**
   - Add rate limiting
   - Implement HTTPS only
   - Add CORS policies
   - Regular security audits

---

## Technology Stack

### Backend (API)
- **Framework**: ASP.NET Core 9.0
- **Authentication**: JWT Bearer
- **ORM**: Entity Framework Core 9.0
- **Database**: SQLite (Dev), SQL Server (Prod)
- **Logging**: Serilog
- **Validation**: Data Annotations + FluentValidation

### Frontend (Web)
- **Framework**: Blazor Server
- **UI Library**: Bootstrap 5
- **Real-time**: SignalR
- **HTTP Client**: HttpClientFactory
- **State Management**: Circuit-scoped services

### Development Tools
- **.NET SDK**: 9.0
- **IDE**: Visual Studio / VS Code / Rider
- **Version Control**: Git
- **Package Manager**: NuGet

---

## Database Schema

### Core Tables

#### Users
- Id (PK)
- Username (Unique)
- Email (Unique)
- PasswordHash
- FullName
- CreatedAt

#### TaxRecords
- Id (PK)
- UserId (FK → Users)
- TaxYear
- TotalIncome
- TotalDeductions
- TaxableIncome
- TotalTax
- EffectiveRate
- IsProcessed
- CreatedAt
- UpdatedAt

#### IncomeSources
- Id (PK)
- TaxRecordId (FK → TaxRecords)
- SourceType (Employment, Business, Investment, etc.)
- Description
- Amount
- MonthlyBreakdownEnabled

#### MonthlyIncome
- Id (PK)
- IncomeSourceId (FK → IncomeSources)
- Month (1-12)
- Amount

#### Deductions
- Id (PK)
- TaxRecordId (FK → TaxRecords)
- DeductionType (NHF, NHIS, Pension, etc.)
- Description
- Amount

#### GeneralLedger
- Id (PK)
- UserId (FK → Users)
- EntryDate
- Description
- Category
- Amount
- EntryType (Income/Expense)
- CreatedAt
- UpdatedAt

---

## Conclusion

NasosoTax follows a well-architected, scalable design that adheres to industry best practices. The clean separation between frontend and backend, combined with proper layering and dependency management, makes the application maintainable, testable, and ready for production deployment.

**Architecture Status:** ✅ **Production Ready**

---

**Document Version:** 1.0  
**Last Updated:** October 2025  
**Maintained By:** Development Team
