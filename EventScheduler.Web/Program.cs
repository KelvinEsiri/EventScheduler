using EventScheduler.Web.Components;
using EventScheduler.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;

// Configure Serilog for Web Application
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "EventScheduler.Web")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/eventscheduler-web-.log",
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
    Log.Information("Starting EventScheduler Web Application");
    Log.Information("========================================");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ═══════════════════════════════════════════════════════════════
// SESSION SUPPORT FOR AUTHENTICATION PERSISTENCE
// ═══════════════════════════════════════════════════════════════
// Add Session support for authentication persistence across reconnections
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24); // Session timeout - matches JWT expiration
    options.Cookie.HttpOnly = true; // Prevents JavaScript access - security
    options.Cookie.IsEssential = true; // Required for functionality
    options.Cookie.Name = ".EventScheduler.Session"; // Consistent session cookie name
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS only in production
});

builder.Services.AddHttpContextAccessor();

// Configure HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5006") 
});

// ═══════════════════════════════════════════════════════════════
// AUTHENTICATION STATE MANAGEMENT - THREE-TIER PERSISTENCE
// ═══════════════════════════════════════════════════════════════

// ⚡ CRITICAL: Register AuthStateCache as SINGLETON
// This ensures the cache survives circuit recreations (reconnections)
// The cache persists across all user sessions on the server
builder.Services.AddSingleton<AuthStateCache>();

// Register background service for cache cleanup
// Runs every 30 minutes to remove expired entries
builder.Services.AddHostedService<AuthCacheCleanupService>();

// Register custom services
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<EventUIHelperService>();

// Register offline support services
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<OfflineEventService>();

// Register AuthStateProvider as SCOPED (per Blazor circuit)
// New instance created for each SignalR circuit
// RestoreFromCache() runs in constructor to restore auth state
builder.Services.AddScoped<AuthStateProvider>();

// Register as AuthenticationStateProvider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthStateProvider>());

// Add Cascading Authentication State for Blazor components
// This makes auth state available to all components via [CascadingParameter]
builder.Services.AddCascadingAuthenticationState();

// Add authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// ⚡ CRITICAL: Enable static files from wwwroot (JS, CSS, images)
// This allows serving reconnection-handler.js and other static files
app.UseStaticFiles();

// ⚡ CRITICAL: Enable Session middleware BEFORE Blazor components
// This ensures session cookie is available when AuthStateProvider is created
// Session middleware must come before MapRazorComponents
app.UseSession();

app.UseAntiforgery();

app.UseSerilogRequestLogging();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var apiUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5005";
Log.Information("EventScheduler Web listening on http://localhost:5292");
Log.Information("Connected to API at: {ApiUrl}", apiUrl);
Log.Information("Web application is ready");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Web application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down EventScheduler Web");
    Log.CloseAndFlush();
}
