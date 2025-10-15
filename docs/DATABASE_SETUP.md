# Database Setup Guide - Event Scheduler

## Overview

Event Scheduler uses **SQL Server** (LocalDB for development) with **Entity Framework Core** migrations for database management.

---

## Database Technology

- **DBMS**: Microsoft SQL Server
- **Development**: SQL Server LocalDB (included with Visual Studio)
- **Production**: SQL Server Express or Full Edition
- **ORM**: Entity Framework Core 9.0
- **Migration Strategy**: Code-First with EF Migrations

---

## Connection Strings

### Development Environment

**Location**: `EventScheduler.Api/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EventSchedulerDb_Dev;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

### Production Environment

**Location**: `EventScheduler.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EventSchedulerDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

### Connection String Parameters

| Parameter | Description |
|-----------|-------------|
| `Server=(localdb)\\mssqllocaldb` | SQL Server LocalDB instance |
| `Database=EventSchedulerDb_Dev` | Database name |
| `Trusted_Connection=true` | Use Windows Authentication |
| `MultipleActiveResultSets=true` | Allow multiple active result sets |
| `TrustServerCertificate=true` | Trust the server certificate (dev only) |

---

## Database Schema

### Tables

#### 1. Users
Stores user account information and authentication data.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique user identifier |
| Username | nvarchar(50) | NOT NULL, Unique | User's username |
| Email | nvarchar(100) | NOT NULL, Unique | User's email address |
| PasswordHash | nvarchar(MAX) | NOT NULL | Hashed password (PBKDF2) |
| FullName | nvarchar(100) | NOT NULL | User's full name |
| CreatedAt | datetime2 | NOT NULL | Account creation timestamp |
| LastLoginAt | datetime2 | NULL | Last login timestamp |
| EmailVerified | bit | NOT NULL | Email verification status |
| PasswordResetToken | nvarchar(MAX) | NULL | Password reset token |
| PasswordResetTokenExpiry | datetime2 | NULL | Token expiration time |

**Indexes:**
- `IX_Users_Username` (Unique)
- `IX_Users_Email` (Unique)

#### 2. Events
Stores event/schedule information.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique event identifier |
| Title | nvarchar(200) | NOT NULL | Event title |
| Description | nvarchar(1000) | NULL | Event description |
| StartDate | datetime2 | NOT NULL | Event start date/time |
| EndDate | datetime2 | NOT NULL | Event end date/time |
| Location | nvarchar(200) | NULL | Event location |
| IsAllDay | bit | NOT NULL | All-day event flag |
| Color | nvarchar(50) | NULL | Display color |
| Status | int | NOT NULL | Event status enum |
| UserId | int | FK, NOT NULL | Owner user ID |
| CategoryId | int | FK, NULL | Category ID |
| CreatedAt | datetime2 | NOT NULL | Creation timestamp |
| UpdatedAt | datetime2 | NULL | Last update timestamp |

**Indexes:**
- `IX_Events_UserId`
- `IX_Events_StartDate`

**Foreign Keys:**
- `FK_Events_Users_UserId` → Users.Id (NO ACTION)
- `FK_Events_EventCategories_CategoryId` → EventCategories.Id (SET NULL)

#### 3. EventCategories
Stores event categories for organization.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique category identifier |
| Name | nvarchar(100) | NOT NULL | Category name |
| Description | nvarchar(500) | NULL | Category description |
| Color | nvarchar(50) | NULL | Display color |
| UserId | int | FK, NOT NULL | Owner user ID |
| CreatedAt | datetime2 | NOT NULL | Creation timestamp |

**Indexes:**
- `IX_EventCategories_UserId`

**Foreign Keys:**
- `FK_EventCategories_Users_UserId` → Users.Id (CASCADE)

---

## Entity Framework Migrations

### Current Migrations

1. **InitialCreate** (`20251015003501_InitialCreate.cs`)
   - Creates Users table with authentication fields
   - Creates Events table with scheduling fields
   - Creates EventCategories table
   - Configures relationships and indexes

### Migration Commands

#### Create a New Migration
```bash
cd EventScheduler.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../EventScheduler.Api
```

#### Apply Migrations to Database
```bash
cd EventScheduler.Infrastructure
dotnet ef database update --startup-project ../EventScheduler.Api
```

#### Rollback to Specific Migration
```bash
cd EventScheduler.Infrastructure
dotnet ef database update PreviousMigrationName --startup-project ../EventScheduler.Api
```

#### Remove Last Migration (if not applied)
```bash
cd EventScheduler.Infrastructure
dotnet ef migrations remove --startup-project ../EventScheduler.Api
```

#### Drop Database
```bash
cd EventScheduler.Infrastructure
dotnet ef database drop --startup-project ../EventScheduler.Api --force
```

#### View Migration SQL Script
```bash
cd EventScheduler.Infrastructure
dotnet ef migrations script --startup-project ../EventScheduler.Api
```

---

## Automatic Migration on Startup

The API automatically applies pending migrations on startup:

**Location**: `EventScheduler.Api/Program.cs`

```csharp
// Database initialization with migrations
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<EventSchedulerDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Applying database migrations...");
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully!");
        
        var userCount = db.Users.Count();
        logger.LogInformation($"Database ready. Current users: {userCount}");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating the database.");
    throw;
}
```

---

## Database Context Configuration

**Location**: `EventScheduler.Infrastructure/Data/EventSchedulerDbContext.cs`

The DbContext configures:
- Entity relationships
- Indexes for performance
- Delete behaviors
- Property constraints
- Navigation properties

### Key Configurations

```csharp
// User unique constraints
modelBuilder.Entity<User>(entity =>
{
    entity.HasIndex(e => e.Username).IsUnique();
    entity.HasIndex(e => e.Email).IsUnique();
});

// Event relationships
modelBuilder.Entity<Event>(entity =>
{
    entity.HasOne(e => e.User)
        .WithMany(u => u.Events)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.NoAction);
});
```

---

## Switching to Different SQL Server Instances

### SQL Server Express

Update connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=EventSchedulerDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
  }
}
```

### Full SQL Server with SQL Authentication

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server-name;Database=EventSchedulerDb;User Id=your-username;Password=your-password;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

### Azure SQL Database

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=EventSchedulerDb;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

---

## Prerequisites

### SQL Server LocalDB

LocalDB is included with:
- Visual Studio 2017 or later
- SQL Server Express 2017 or later

**Verify Installation:**
```bash
sqllocaldb info
```

**Create LocalDB Instance (if needed):**
```bash
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

### Required NuGet Packages

**EventScheduler.Infrastructure:**
- `Microsoft.EntityFrameworkCore` (9.0.10)
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.10)
- `Microsoft.EntityFrameworkCore.Design` (9.0.10)

**EventScheduler.Api:**
- `Microsoft.EntityFrameworkCore.Design` (9.0.10)

---

## Database Backup and Restore

### Backup Database

```sql
BACKUP DATABASE EventSchedulerDb_Dev
TO DISK = 'C:\Backups\EventSchedulerDb_Dev.bak'
WITH FORMAT;
```

### Restore Database

```sql
RESTORE DATABASE EventSchedulerDb_Dev
FROM DISK = 'C:\Backups\EventSchedulerDb_Dev.bak'
WITH REPLACE;
```

---

## Troubleshooting

### Issue: LocalDB Not Running

**Solution:**
```bash
sqllocaldb start MSSQLLocalDB
```

### Issue: Database Connection Fails

**Solution:**
1. Check if SQL Server LocalDB is installed
2. Verify connection string in appsettings.json
3. Check Windows Event Viewer for SQL Server errors

### Issue: Migration Fails - Table Already Exists

**Solution:**
```bash
# Drop the database
dotnet ef database drop --force --project EventScheduler.Infrastructure --startup-project EventScheduler.Api

# Run the app to recreate with migrations
cd EventScheduler.Api
dotnet run
```

### Issue: Permission Denied

**Solution:**
- Ensure your Windows user has permissions
- Run Visual Studio or terminal as Administrator (if needed)
- Check SQL Server service is running

### Issue: Cannot See Database in SQL Server Management Studio (SSMS)

**Solution:**
1. Connect to `(localdb)\MSSQLLocalDB`
2. Make sure LocalDB instance is running
3. Use SQL Server Object Explorer in Visual Studio

---

## Performance Optimization

### Recommended Indexes

The following indexes are already configured:

```csharp
// User lookups
modelBuilder.Entity<User>()
    .HasIndex(e => e.Username).IsUnique();
modelBuilder.Entity<User>()
    .HasIndex(e => e.Email).IsUnique();

// Event queries
modelBuilder.Entity<Event>()
    .HasIndex(e => e.UserId);
modelBuilder.Entity<Event>()
    .HasIndex(e => e.StartDate);

// Category queries
modelBuilder.Entity<EventCategory>()
    .HasIndex(e => e.UserId);
```

### Additional Optimization Tips

1. **Enable Query Logging** (Development only):
```csharp
builder.Services.AddDbContext<EventSchedulerDbContext>(options =>
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging()
           .EnableDetailedErrors());
```

2. **Connection Pooling**: Enabled by default in SQL Server

3. **Asynchronous Operations**: Already used throughout the application

---

## Security Best Practices

### ✅ Implemented

- ✅ Parameterized queries (via EF Core)
- ✅ Password hashing (PBKDF2)
- ✅ SQL injection prevention (via EF Core)
- ✅ Connection string in configuration files
- ✅ No sensitive data in logs

### 🔒 Production Recommendations

1. **Use Environment Variables** for connection strings
2. **Enable encryption** in connection string
3. **Use Azure Key Vault** for secrets
4. **Regular backups** automated
5. **Monitor failed login attempts**
6. **Implement rate limiting** on API

---

## Database Seed Data (Optional)

To add seed data, create a migration:

```csharp
// In a new migration
migrationBuilder.InsertData(
    table: "EventCategories",
    columns: new[] { "Name", "Description", "Color", "UserId", "CreatedAt" },
    values: new object[] { "Work", "Work-related events", "#007bff", 1, DateTime.UtcNow }
);
```

---

## Monitoring and Maintenance

### View Database Size

```sql
SELECT 
    DB_NAME() AS DatabaseName,
    (SUM(size) * 8 / 1024) AS SizeMB
FROM sys.master_files
WHERE database_id = DB_ID()
GROUP BY database_id;
```

### View Table Row Counts

```sql
SELECT 
    t.NAME AS TableName,
    p.rows AS RowCount
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.OBJECT_ID
WHERE t.is_ms_shipped = 0
GROUP BY t.Name, p.Rows
ORDER BY p.rows DESC;
```

### Check Index Usage

```sql
SELECT 
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE database_id = DB_ID();
```

---

## Conclusion

The Event Scheduler database is fully configured with:
- ✅ SQL Server LocalDB for development
- ✅ Entity Framework Core migrations
- ✅ Automatic migration on startup
- ✅ Proper indexes and relationships
- ✅ Security best practices

For production deployment, update the connection string to point to your production SQL Server instance and ensure proper backups are in place.

---

**Last Updated**: October 15, 2025  
**Database Version**: Initial (Migration: 20251015003501_InitialCreate)  
**EF Core Version**: 9.0.10
