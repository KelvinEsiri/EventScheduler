# Quick API Restart for CORS Fix
# Rebuilds and restarts only the API server

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "API CORS Fix - Rebuild & Restart" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Issue: Health check endpoint blocked by CORS" -ForegroundColor Yellow
Write-Host "Fix: Added http://127.0.0.1:5292 to allowed origins" -ForegroundColor Green
Write-Host ""

# Step 1: Stop API
Write-Host "Step 1: Stopping API server..." -ForegroundColor Yellow
$apiProcess = Get-NetTCPConnection -LocalPort 5006 -ErrorAction SilentlyContinue | 
              Select-Object -ExpandProperty OwningProcess | 
              Get-Process -ErrorAction SilentlyContinue

if ($apiProcess) {
    Stop-Process -Id $apiProcess.Id -Force
    Start-Sleep -Seconds 2
    Write-Host "✓ API server stopped" -ForegroundColor Green
} else {
    Write-Host "⚠ API server not running" -ForegroundColor Yellow
}
Write-Host ""

# Step 2: Rebuild API
Write-Host "Step 2: Rebuilding API..." -ForegroundColor Yellow
dotnet build EventScheduler.Api\EventScheduler.Api.csproj --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
Write-Host "✓ API rebuilt successfully" -ForegroundColor Green
Write-Host ""

# Step 3: Restart API
Write-Host "Step 3: Restarting API server..." -ForegroundColor Yellow
Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$PWD\EventScheduler.Api'; dotnet run" -WindowStyle Normal
Start-Sleep -Seconds 5
Write-Host "✓ API server starting..." -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ API Restart Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API: http://localhost:5006" -ForegroundColor Cyan
Write-Host "Health: http://localhost:5006/health" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Wait ~10 seconds for API to fully start" -ForegroundColor Gray
Write-Host "2. Refresh browser (Ctrl+Shift+R)" -ForegroundColor Gray
Write-Host "3. Check console - should see:" -ForegroundColor Gray
Write-Host "   [NetworkStatus] Server is now reachable" -ForegroundColor DarkGray
Write-Host "   [NetworkStatus] Status: ONLINE" -ForegroundColor DarkGray
Write-Host ""
Write-Host "If CORS errors persist, check API logs for any startup errors." -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
