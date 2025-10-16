using EventScheduler.Api.Middleware;
using EventScheduler.Api.Hubs;
using EventScheduler.Infrastructure.Data;
using EventScheduler.Infrastructure.Repositories;
using EventScheduler.Application.Interfaces.Repositories;
using EventScheduler.Application.Interfaces.Services;
using EventScheduler.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with detailed logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "EventScheduler.API")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/eventscheduler-api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        fileSizeLimitBytes: 10_000_000,
        rollOnFileSizeLimit: true,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(2),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("========================================");
    Log.Information("Starting EventScheduler API");
    Log.Information("========================================");

    builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add SignalR
Log.Information("Configuring SignalR...");
builder.Services.AddSignalR();
Log.Information("✅ SignalR configured successfully");

// Configure Database - Using SQLite for development
Log.Information("Configuring database with SQLite");
builder.Services.AddDbContext<EventSchedulerDbContext>(options =>
    options.UseSqlite("Data Source=EventScheduler.db"));

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventNotificationService, EventScheduler.Api.Services.EventNotificationService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EventScheduler";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EventScheduler";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // Configure JWT for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5292", 
                    "https://localhost:7248",
                    "http://localhost:5006",
                    "https://localhost:7249")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

// Use custom error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseCors("AllowWebApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("Mapping SignalR hub to /hubs/events...");
app.MapHub<EventHub>("/hubs/events");
Log.Information("✅ SignalR hub endpoint configured at: /hubs/events");

// Add a simple health check endpoint
app.MapGet("/", () => "EventScheduler API is running!");

// Database initialization
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<EventSchedulerDbContext>();
        
        Log.Information("Ensuring database is created...");
        db.Database.EnsureCreated();
        Log.Information("Database is ready!");
        
        var userCount = db.Users.Count();
        var eventCount = db.Events.Count();
        Log.Information("Database ready - Users: {UserCount}, Events: {EventCount}", userCount, eventCount);
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Database initialization failed!");
    throw;
}

Log.Information("========================================");
Log.Information("🚀 EventScheduler API is READY");
Log.Information("========================================");
Log.Information("API listening on: http://localhost:5006");
Log.Information("SignalR Hub at: http://localhost:5006/hubs/events");
Log.Information("Health check: http://localhost:5006");
Log.Information("========================================");
Log.Information("✅ API is ready to accept requests");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down EventScheduler API");
    Log.CloseAndFlush();
}
