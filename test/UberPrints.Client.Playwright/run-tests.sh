#!/bin/bash

# Script to start servers and run Playwright tests

echo "🚀 Starting UberPrints servers and running E2E tests..."

# Function to cleanup background processes
cleanup() {
    echo ""
    echo "🛑 Shutting down servers..."
    kill $BACKEND_PID 2>/dev/null
    kill $FRONTEND_PID 2>/dev/null
    exit
}

# Trap SIGINT (Ctrl+C) and EXIT
trap cleanup SIGINT EXIT

# Check if backend server is already running
if curl -k -s https://localhost:7001 > /dev/null 2>&1; then
    echo "✓ Backend server already running at https://localhost:7001"
    BACKEND_RUNNING=true
else
    echo "📦 Starting backend server..."
    cd ../../src/UberPrints.Server
    dotnet run > /dev/null 2>&1 &
    BACKEND_PID=$!
    cd - > /dev/null
    echo "⏳ Waiting for backend to start..."

    # Wait for backend (max 60 seconds)
    for i in {1..60}; do
        if curl -k -s https://localhost:7001 > /dev/null 2>&1; then
            echo "✓ Backend server ready at https://localhost:7001"
            break
        fi
        sleep 1
        if [ $i -eq 60 ]; then
            echo "❌ Backend server failed to start"
            exit 1
        fi
    done
fi

# Check if frontend server is already running
if curl -s http://localhost:5173 > /dev/null 2>&1; then
    echo "✓ Frontend server already running at http://localhost:5173"
    FRONTEND_RUNNING=true
else
    echo "🌐 Starting frontend server..."
    cd ../../src/UberPrints.Client
    npm run dev > /dev/null 2>&1 &
    FRONTEND_PID=$!
    cd - > /dev/null
    echo "⏳ Waiting for frontend to start..."

    # Wait for frontend (max 60 seconds)
    for i in {1..60}; do
        if curl -s http://localhost:5173 > /dev/null 2>&1; then
            echo "✓ Frontend server ready at http://localhost:5173"
            break
        fi
        sleep 1
        if [ $i -eq 60 ]; then
            echo "❌ Frontend server failed to start"
            exit 1
        fi
    done
fi

echo ""
echo "🎭 Running Playwright tests..."
npm test

# Cleanup will happen automatically via trap
