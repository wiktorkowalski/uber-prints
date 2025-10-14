#!/bin/bash

# UberPrints Playwright E2E Tests Setup Script

echo "🎭 Setting up Playwright E2E tests for UberPrints..."

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js 18 or higher."
    exit 1
fi

echo "✓ Node.js $(node --version) found"

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo "❌ npm is not installed. Please install npm."
    exit 1
fi

echo "✓ npm $(npm --version) found"

# Install dependencies
echo "📦 Installing dependencies..."
npm install

# Install Playwright browsers
echo "🌐 Installing Playwright browsers..."
npx playwright install

# Install system dependencies for Playwright
echo "🔧 Installing system dependencies..."
npx playwright install-deps

echo ""
echo "✅ Setup complete!"
echo ""
echo "Available commands:"
echo "  npm test              - Run all tests (headless)"
echo "  npm run test:ui       - Run tests with UI mode"
echo "  npm run test:headed   - Run tests in headed mode"
echo "  npm run test:debug    - Run tests in debug mode"
echo "  npm run report        - Show HTML test report"
echo ""
echo "To run tests:"
echo "  cd test/UberPrints.Client.Playwright"
echo "  npm test"
echo ""
