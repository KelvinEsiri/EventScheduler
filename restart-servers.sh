#!/bin/bash
# Restart Both Servers Script for Linux/Mac

echo "========================================="
echo "Restarting EventScheduler Servers"
echo "========================================="

# Kill existing processes
echo "Stopping existing servers..."
pkill -f "EventScheduler.Api"
pkill -f "EventScheduler.Web"

sleep 2

echo ""
echo "Starting API Server..."
cd EventScheduler.Api
dotnet run &
API_PID=$!

echo "API Server started (PID: $API_PID)"
echo "Waiting for API to initialize..."
sleep 5

echo ""
echo "Starting Web Server..."
cd ../EventScheduler.Web
dotnet run &
WEB_PID=$!

echo "Web Server started (PID: $WEB_PID)"
echo ""
echo "========================================="
echo "Both servers are starting..."
echo "API:  http://localhost:5005"
echo "Web:  http://localhost:5292"
echo "========================================="
echo ""
echo "Press Ctrl+C to stop both servers"
echo ""

# Wait for both processes
wait $API_PID $WEB_PID
