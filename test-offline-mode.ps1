# Test Offline Mode - Server Down Detection

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Offline Mode - Server Detection" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "This script will help you test the enhanced offline mode detection." -ForegroundColor Yellow
Write-Host ""

# Function to check if process is running
function Test-ServerRunning {
    param($port)
    $connection = Test-NetConnection -ComputerName localhost -Port $port -WarningAction SilentlyContinue -InformationLevel Quiet
    return $connection
}

Write-Host "Step 1: Checking Current Server Status..." -ForegroundColor Yellow
Write-Host ""

$apiRunning = Test-ServerRunning -port 5006
$webRunning = Test-ServerRunning -port 5292

if ($apiRunning) {
    Write-Host "  ✅ API Server is running on port 5006" -ForegroundColor Green
} else {
    Write-Host "  ❌ API Server is NOT running on port 5006" -ForegroundColor Red
}

if ($webRunning) {
    Write-Host "  ✅ Web Server is running on port 5292" -ForegroundColor Green
} else {
    Write-Host "  ❌ Web Server is NOT running on port 5292" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Scenario 1: Stop API Server" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will test if offline mode activates when the API server stops." -ForegroundColor Yellow
Write-Host ""
Write-Host "Instructions:" -ForegroundColor White
Write-Host "1. Keep your browser open with the EventScheduler app" -ForegroundColor Gray
Write-Host "2. Open DevTools Console (F12)" -ForegroundColor Gray
Write-Host "3. Press Enter to stop the API server" -ForegroundColor Gray
Write-Host "4. Watch the console for offline detection messages" -ForegroundColor Gray
Write-Host "5. After 5 seconds, you should see:" -ForegroundColor Gray
Write-Host "   [NetworkStatus] Server health check failed" -ForegroundColor DarkGray
Write-Host "   [NetworkStatus] Server is unreachable" -ForegroundColor DarkGray
Write-Host "   [NetworkStatus] Status: OFFLINE" -ForegroundColor DarkGray
Write-Host ""

$continue = Read-Host "Ready to stop API server? (y/n)"

if ($continue -eq 'y') {
    Write-Host ""
    Write-Host "Stopping API server..." -ForegroundColor Yellow
    
    # Find and stop dotnet process on port 5006
    $apiProcess = Get-NetTCPConnection -LocalPort 5006 -ErrorAction SilentlyContinue | 
                  Select-Object -ExpandProperty OwningProcess | 
                  Get-Process -ErrorAction SilentlyContinue
    
    if ($apiProcess) {
        Stop-Process -Id $apiProcess.Id -Force
        Write-Host "✅ API Server stopped!" -ForegroundColor Green
    } else {
        Write-Host "❌ Could not find API server process" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "⏱️  Waiting 10 seconds..." -ForegroundColor Yellow
    Write-Host "   (Watch your browser console for offline detection)" -ForegroundColor Gray
    
    for ($i = 10; $i -gt 0; $i--) {
        Write-Host "   $i..." -ForegroundColor DarkGray
        Start-Sleep -Seconds 1
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Test Scenario 2: Restart API Server" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "This will test if the app auto-reconnects when server comes back." -ForegroundColor Yellow
    Write-Host ""
    
    $restart = Read-Host "Ready to restart API server? (y/n)"
    
    if ($restart -eq 'y') {
        Write-Host ""
        Write-Host "Restarting API server..." -ForegroundColor Yellow
        
        Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$PWD\EventScheduler.Api'; dotnet run" -WindowStyle Normal
        
        Write-Host "✅ API Server restarting..." -ForegroundColor Green
        Write-Host ""
        Write-Host "⏱️  Waiting 10 seconds for startup..." -ForegroundColor Yellow
        Write-Host "   (Watch your browser console for reconnection)" -ForegroundColor Gray
        
        for ($i = 10; $i -gt 0; $i--) {
            Write-Host "   $i..." -ForegroundColor DarkGray
            Start-Sleep -Seconds 1
        }
        
        Write-Host ""
        Write-Host "Expected Console Messages:" -ForegroundColor Yellow
        Write-Host "  [NetworkStatus] Server is now reachable" -ForegroundColor DarkGray
        Write-Host "  [NetworkStatus] Status: ONLINE" -ForegroundColor DarkGray
        Write-Host "  [OfflineSync] Syncing pending operations..." -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ Test Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "What to verify in browser console:" -ForegroundColor Yellow
Write-Host "  ✅ Offline mode activated when API stopped" -ForegroundColor Green
Write-Host "  ✅ Online mode restored when API restarted" -ForegroundColor Green
Write-Host "  ✅ Pending operations synced automatically" -ForegroundColor Green
Write-Host "  ✅ Events loaded from server after reconnect" -ForegroundColor Green
Write-Host ""
Write-Host "If you see all of these, the offline mode is working perfectly! 🎉" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
