@echo off
REM Restart Both Servers Script for Windows

echo =========================================
echo Restarting EventScheduler Servers
echo =========================================

REM Kill existing processes
echo Stopping existing servers...
taskkill /F /IM EventScheduler.Api.exe 2>nul
taskkill /F /IM EventScheduler.Web.exe 2>nul
timeout /t 2 /nobreak >nul

echo.
echo Starting API Server...
cd EventScheduler.Api
start "EventScheduler API" cmd /k "dotnet run"

echo API Server started
echo Waiting for API to initialize...
timeout /t 5 /nobreak >nul

echo.
echo Starting Web Server...
cd ..\EventScheduler.Web
start "EventScheduler Web" cmd /k "dotnet run"

echo Web Server started
echo.
echo =========================================
echo Both servers are starting in new windows
echo API:  http://localhost:5005
echo Web:  http://localhost:5292
echo =========================================
echo.
echo Close the terminal windows to stop the servers
echo.
pause
