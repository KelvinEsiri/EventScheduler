# Running the Separated Architecture

The NasosoTax application now consists of two separate projects that need to run simultaneously:

1. **NasosoTax.Api** - Backend API (Controllers, Business Logic, Database)
2. **NasosoTax.Web** - Frontend (Blazor Server UI)

## Architecture Overview

```
┌──────────────────────────────────────┐
│   NasosoTax.Web (Frontend)          │
│   Port: 5070                        │
│   - Blazor Server Components        │
│   - UI Pages                        │
│   - Client Services                 │
└──────────┬───────────────────────────┘
           │
           │ HTTP API Calls
           │
┌──────────▼───────────────────────────┐
│   NasosoTax.Api (Backend)           │
│   Port: 5001                        │
│   - Controllers                     │
│   - Authentication                  │
│   - Business Logic                  │
│   - Database Access                 │
└─────────────────────────────────────┘
```

## Running Both Projects

### Quick Start Scripts (Easiest)

**Linux/Mac:**
```bash
./run-all.sh
```

**Windows:**
```cmd
run-all.bat
```

These scripts will automatically start both the API and Web projects in the correct order.

### Option 1: Using Multiple Terminals (Recommended for Development)

**Terminal 1 - Start the API:**
```bash
cd NasosoTax.Api
dotnet run
```

The API will start on: `http://localhost:5001`

**Terminal 2 - Start the Web Frontend:**
```bash
cd NasosoTax.Web
dotnet run
```

The Web UI will start on: `http://localhost:5070`

**Access the application:** Open your browser and navigate to `http://localhost:5070`

### Option 2: Using dotnet watch for Auto-Reload

For development with automatic reload on file changes:

**Terminal 1 - API with watch:**
```bash
cd NasosoTax.Api
dotnet watch run
```

**Terminal 2 - Web with watch:**
```bash
cd NasosoTax.Web
dotnet watch run
```

### Option 3: Using Visual Studio or VS Code

If you have a launch configuration, you can start both projects simultaneously:

**Visual Studio:**
1. Right-click the solution
2. Set Multiple Startup Projects
3. Set both NasosoTax.Api and NasosoTax.Web to "Start"
4. Press F5

**VS Code:**
Create a `.vscode/tasks.json` to run both projects:
```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "run-api",
            "command": "dotnet",
            "type": "process",
            "args": [
                "run",
                "--project",
                "${workspaceFolder}/NasosoTax.Api/NasosoTax.Api.csproj"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "run-web",
            "command": "dotnet",
            "type": "process",
            "args": [
                "run",
                "--project",
                "${workspaceFolder}/NasosoTax.Web/NasosoTax.Web.csproj"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "run-both",
            "dependsOn": ["run-api", "run-web"]
        }
    ]
}
```

## Configuration

### API Configuration (NasosoTax.Api/appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=nasosotax.db"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyForNasosoTaxPortal2025MinimumLength32Characters!",
    "Issuer": "NasosoTax",
    "Audience": "NasosoTaxUsers"
  }
}
```

### Web Configuration (NasosoTax.Web/appsettings.json)
```json
{
  "ApiBaseUrl": "http://localhost:5001"
}
```

**Important:** The `ApiBaseUrl` in the Web project must match the URL where the API is running.

## Project Structure

### NasosoTax.Api (Backend)
- `Controllers/` - API endpoints
  - `AuthController.cs` - Login, Register
  - `TaxController.cs` - Tax calculations and records
  - `LedgerController.cs` - General ledger operations
  - `ReportsController.cs` - Tax reports
  - `HealthController.cs` - Health checks
- `Middleware/` - Error handling
- `Program.cs` - API configuration
- Database migrations are applied here

### NasosoTax.Web (Frontend)
- `Components/Pages/` - Blazor pages
  - `Home.razor`, `Calculator.razor`, `Login.razor`, etc.
- `Services/` - Frontend services
  - `ApiService.cs` - HTTP client for API calls
  - `AuthStateProvider.cs` - Authentication state
- `Program.cs` - Web UI configuration

## Troubleshooting

### Issue: Web cannot connect to API
**Solution:** Ensure the API is running and the `ApiBaseUrl` in Web's appsettings.json is correct.

### Issue: Database not found
**Solution:** The API project handles database migrations. Make sure you run the API first.

### Issue: Port already in use
**Solution:** You can change the ports in `Properties/launchSettings.json` for each project.

### Issue: CORS errors
**Solution:** The API has CORS configured to allow all origins. If you change the API URL, ensure CORS is properly configured.

## Benefits of Separated Architecture

1. **Clear Separation of Concerns** - Frontend and backend are completely independent
2. **Scalability** - Can deploy API and Web separately
3. **Testing** - Easier to test API independently
4. **Multiple Frontends** - Can build mobile apps, SPAs, etc. using the same API
5. **Team Organization** - Frontend and backend teams can work independently
6. **Security** - Better control over what frontend can access

## Migration from Previous Architecture

Previously, both controllers and Blazor components were in the same `NasosoTax.Web` project. Now:

- **API Controllers** moved to `NasosoTax.Api` project
- **Blazor Components** remain in `NasosoTax.Web` project
- **Communication** happens via HTTP/REST APIs
- **Authentication** handled by API with JWT tokens
- **Database** managed by API project
