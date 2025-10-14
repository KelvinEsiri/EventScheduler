#!/bin/bash

# Event Scheduler - Run All Services
# This script starts both the API and Web application

echo "Starting Event Scheduler..."
echo ""

# Start API in background
echo "Starting API on http://localhost:5001..."
cd EventScheduler.Api
dotnet run --urls="http://localhost:5001" &
API_PID=$!
cd ..

# Wait for API to start
sleep 5

# Start Web application
echo ""
echo "Starting Web App on http://localhost:5070..."
cd EventScheduler.Web
dotnet run --urls="http://localhost:5070"
WEB_PID=$!
cd ..

# Cleanup on exit
trap "kill $API_PID $WEB_PID" EXIT

wait
