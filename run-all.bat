@echo off
REM Event Scheduler - Run All Services
REM This script starts both the API and Web application

echo Starting Event Scheduler...
echo.

REM Start API
echo Starting API on http://localhost:5001...
start "Event Scheduler API" dotnet run --project EventScheduler.Api\EventScheduler.Api.csproj --urls="http://localhost:5001"

REM Wait a bit for API to start
timeout /t 5 /nobreak

REM Start Web application
echo.
echo Starting Web App on http://localhost:5070...
start "Event Scheduler Web" dotnet run --project EventScheduler.Web\EventScheduler.Web.csproj --urls="http://localhost:5070"

echo.
echo Both services are starting...
echo API: http://localhost:5001
echo Web: http://localhost:5070
echo.
pause
