# Fix and Restart Script for EventScheduler
# This script stops servers, rebuilds, and restarts with fixes applied

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "EventScheduler - Fix and Restart" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop any running processes
Write-Host "Step 1: Stopping any running .NET processes..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Write-Host "✓ Processes stopped" -ForegroundColor Green
Write-Host ""

# Step 2: Clean build directories
Write-Host "Step 2: Cleaning build artifacts..." -ForegroundColor Yellow
if (Test-Path "EventScheduler.Api\bin") {
    Remove-Item -Path "EventScheduler.Api\bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "EventScheduler.Api\obj") {
    Remove-Item -Path "EventScheduler.Api\obj" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "EventScheduler.Web\bin") {
    Remove-Item -Path "EventScheduler.Web\bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "EventScheduler.Web\obj") {
    Remove-Item -Path "EventScheduler.Web\obj" -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "✓ Build artifacts cleaned" -ForegroundColor Green
Write-Host ""

# Step 3: Rebuild the solution
Write-Host "Step 3: Rebuilding solution..." -ForegroundColor Yellow
dotnet build EventScheduler.sln --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build successful" -ForegroundColor Green
Write-Host ""

# Step 4: Clear browser cache instructions
Write-Host "Step 4: IMPORTANT - Clear Browser Cache!" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "Before testing, open browser DevTools console and run:" -ForegroundColor Yellow
Write-Host "  indexedDB.deleteDatabase('EventSchedulerOfflineDB');" -ForegroundColor Cyan
Write-Host "  localStorage.clear();" -ForegroundColor Cyan
Write-Host "  location.reload();" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to continue with server startup..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
Write-Host ""

# Step 5: Start API
Write-Host "Step 5: Starting API server..." -ForegroundColor Yellow
Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$PWD\EventScheduler.Api'; dotnet run" -WindowStyle Normal
Start-Sleep -Seconds 5
Write-Host "✓ API server starting (Port 5006)" -ForegroundColor Green
Write-Host ""

# Step 6: Start Web
Write-Host "Step 6: Starting Web server..." -ForegroundColor Yellow
Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$PWD\EventScheduler.Web'; dotnet run" -WindowStyle Normal
Start-Sleep -Seconds 5
Write-Host "✓ Web server starting (Port 5292)" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "✓ ALL SERVERS STARTED" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API:  http://localhost:5006" -ForegroundColor Cyan
Write-Host "Web:  http://localhost:5292" -ForegroundColor Cyan
Write-Host ""
Write-Host "FIXES APPLIED:" -ForegroundColor Yellow
Write-Host "  ✓ FullCalendar CSS URL corrected" -ForegroundColor Green
Write-Host "  ✓ Removed unnecessary scoped styles reference" -ForegroundColor Green
Write-Host "  ✓ JSON serialization set to camelCase" -ForegroundColor Green
Write-Host "  ✓ IndexedDB id/Id fallback added" -ForegroundColor Green
Write-Host ""
Write-Host "REMEMBER: Clear browser cache before testing!" -ForegroundColor Magenta
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
