# Logging Implementation Guide

## Overview

This project uses **Serilog** for comprehensive logging throughout the application. Logs are written to both the console and file system, with files stored in the `internal/logs/` directory (which is git-ignored).

## Configuration

### Serilog Setup

Serilog is configured in `Program.cs` with the following features:

- **Minimum Log Level**: Information (can be overridden per namespace)
- **Console Logging**: With colored output and simplified format
- **File Logging**: Daily rolling logs with retention policy
- **Request Logging**: Automatic HTTP request/response logging
- **Structured Logging**: All logs include contextual information

### Log File Details

- **Location**: `internal/logs/`
- **File Pattern**: `nasosotax-YYYYMMDD.log`
- **Rolling**: Daily (new file each day)
- **Retention**: 30 days
- **Max File Size**: 10 MB per file

### Example Log Output

**Console Format:**
```
[14:23:45 INF] Login successful for username: john_doe {"UserId": 123}
```

**File Format:**
```
[2025-10-10 14:23:45.123 +00:00 INF] [NasosoTax.Application.Services.AuthService] Login successful for username: john_doe {"UserId": 123}
```

## Log Levels Used

### Information (`LogInformation`)
- User actions (login, registration, data submission)
- Successful operations
- Important state changes

### Debug (`LogDebug`)
- Detailed operational information
- Method parameters and return values
- Cache operations
- Database queries

### Warning (`LogWarning`)
- Failed login attempts
- Unauthorized access attempts
- Missing resources (soft failures)
- Validation failures

### Error (`LogError`)
- Operation failures
- Exception details
- System errors

### Fatal (`LogFatal`)
- Application startup failures
- Critical system errors

## Logging by Layer

### Controllers (`NasosoTax.Web.Controllers`)
- HTTP request initiation
- User authentication status
- Request parameters (sanitized)
- Response status

### Services (`NasosoTax.Application.Services`)
- Business logic operations
- Calculation results
- Service method calls
- Operation outcomes

### Repositories (`NasosoTax.Infrastructure.Repositories`)
- Database operations
- Entity creation/updates
- Query results
- Record counts

## Examples

### AuthService Logging
```csharp
_logger.LogInformation("Login attempt for username: {Username}", request.Username);
_logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
_logger.LogInformation("Successful login for username: {Username}, UserId: {UserId}", user.Username, user.Id);
```

### TaxCalculationService Logging
```csharp
_logger.LogInformation("Calculating tax for totalIncome: {TotalIncome:C}, deductions count: {DeductionCount}", totalIncome, deductions.Count);
_logger.LogDebug("Bracket: {Description}, Income in bracket: {IncomeInBracket:C}, Tax: {TaxInBracket:C}", bracket.Description, incomeInBracket, taxInBracket);
```

### Controller Logging
```csharp
_logger.LogInformation("Submit income and deductions request for UserId: {UserId}, TaxYear: {TaxYear}", userId, request.TaxYear);
_logger.LogWarning("Unauthorized access attempt to submit income and deductions");
```

## Best Practices

1. **Use Structured Logging**: Always use placeholders for variables
   - ✅ `_logger.LogInformation("User {UserId} logged in", userId)`
   - ❌ `_logger.LogInformation($"User {userId} logged in")`

2. **Sensitive Data**: Never log passwords, tokens, or sensitive financial details in plain text

3. **Context**: Include relevant IDs (UserId, RecordId, etc.) for traceability

4. **Appropriate Levels**: Choose the correct log level for the situation

5. **Performance**: Use `LogDebug` for verbose information that might impact performance

## Viewing Logs

### During Development
Logs appear in the console with color-coded levels.

### Production
Access log files in the `internal/logs/` directory:
```powershell
# View today's log
Get-Content "internal/logs/nasosotax-$(Get-Date -Format 'yyyyMMdd').log"

# Tail the log (follow new entries)
Get-Content "internal/logs/nasosotax-$(Get-Date -Format 'yyyyMMdd').log" -Wait -Tail 50

# Search for errors
Select-String -Pattern "ERR|WRN" -Path "internal/logs/*.log"
```

### Linux/Mac
```bash
# View today's log
cat internal/logs/nasosotax-$(date +%Y%m%d).log

# Tail the log
tail -f internal/logs/nasosotax-$(date +%Y%m%d).log

# Search for errors
grep -E "ERR|WRN" internal/logs/*.log
```

## Configuration Options

You can adjust logging behavior in `Program.cs`:

### Change Minimum Level
```csharp
.MinimumLevel.Debug() // More verbose
.MinimumLevel.Warning() // Less verbose
```

### Filter Specific Namespaces
```csharp
.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
```

### Add Additional Sinks
```csharp
.WriteTo.Seq("http://localhost:5341") // Send to Seq
.WriteTo.Email(...) // Email on errors
```

## Troubleshooting

### Logs Not Appearing
1. Check that `internal/logs/` directory exists
2. Verify file permissions
3. Check `MinimumLevel` configuration

### Log Files Growing Too Large
1. Reduce retention period in `Program.cs`
2. Lower `fileSizeLimitBytes`
3. Increase minimum log level

### Performance Issues
1. Reduce Debug-level logging in production
2. Use async operations
3. Consider buffered writing

## Security

- The `internal/` directory is in `.gitignore` to prevent logs from being committed
- Logs may contain user IDs and operational data but not sensitive information
- Review log access permissions in production environments
- Consider log encryption for highly sensitive environments

## Dependencies

- `Serilog.AspNetCore` (v9.0.0)
- `Serilog.Sinks.Console` (included)
- `Serilog.Sinks.File` (included)

## Future Enhancements

Consider adding:
- Centralized log aggregation (e.g., Elasticsearch, Seq)
- Structured log analysis
- Automated log monitoring and alerting
- Performance metrics logging
- User activity analytics
