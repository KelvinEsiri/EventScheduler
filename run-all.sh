#!/bin/bash

# EventScheduler - Startup Script
# Starts both API and Web application

echo "========================================"
echo "  EventScheduler - Starting Services"
echo "========================================"
echo ""

# Start API in background
echo "[1/2] Starting API..."
cd EventScheduler.Api
dotnet run &
API_PID=$!
cd ..

# Wait for API to initialize
echo "      Waiting for API to start..."
sleep 8

# Start Web application in background
echo ""
echo "[2/2] Starting Web App..."
cd EventScheduler.Web
dotnet run &
WEB_PID=$!
cd ..

echo ""
echo "========================================"
echo "  Services Started:"
echo "  - API:  http://localhost:5005"
echo "  - Web:  http://localhost:5292"
echo "========================================"
echo ""
echo "Press Ctrl+C to stop all services"
echo ""

# Cleanup on exit
trap "echo ''; echo 'Stopping services...'; kill $API_PID $WEB_PID 2>/dev/null; echo 'Services stopped.'; exit" INT TERM

# Wait for processes
wait
