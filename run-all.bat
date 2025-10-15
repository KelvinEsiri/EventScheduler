@echo off
REM EventScheduler - Startup Script
REM Starts both API and Web application

echo ========================================
echo   EventScheduler - Starting Services
echo ========================================
echo.

REM Start API
echo [1/2] Starting API...
start "EventScheduler API" cmd /k "cd EventScheduler.Api && dotnet run"

REM Wait for API to initialize
echo       Waiting for API to start...
timeout /t 8 /nobreak > nul

REM Start Web application
echo.
echo [2/2] Starting Web App...
start "EventScheduler Web" cmd /k "cd EventScheduler.Web && dotnet run"

echo.
echo ========================================
echo   Services Starting:
echo   - API:  http://localhost:5005
echo   - Web:  http://localhost:5292
echo ========================================
echo.
echo Press any key to close this window...
echo (Keep the other windows open)
pause > nul
