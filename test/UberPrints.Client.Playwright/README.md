# UberPrints E2E Tests with Playwright

End-to-end tests for the UberPrints client application using Playwright.

## 🚀 Two Test Configurations Available

### 1. **Standard** (Default) - Manual Setup
- ✅ Fast test startup
- ❌ Requires manual backend/database setup
- Best for: Active development

### 2. **Full Orchestration** (Recommended for CI) - **NEW!**
- ✅ **Zero manual setup - just Docker!**
- ✅ Automatic PostgreSQL (Testcontainers)
- ✅ Automatic migrations & seeding
- ✅ Automatic backend & frontend
- Best for: CI/CD, first-time setup

**See [FULL_ORCHESTRATION.md](./FULL_ORCHESTRATION.md) for complete details**

## Quick Start

### Option A: Full Orchestration (Zero Setup Required!)

```bash
# 1. Ensure Docker is running
docker ps

# 2. Install dependencies
npm install
npx playwright install

# 3. Run tests - everything starts automatically!
npm run test:full
```

That's it! PostgreSQL, backend, frontend - all handled automatically.

### Option B: Standard (Manual Setup)

Requires you to start servers manually first (see below).

## Prerequisites

**For Full Orchestration:**
- Node.js 18 or higher
- Docker (for Testcontainers)

**For Standard:**
- Node.js 18 or higher
- PostgreSQL database
- UberPrints backend server (ASP.NET Core)
- UberPrints frontend client (React + Vite)

## Installation

```bash
cd test/UberPrints.Client.Playwright
npm install
npx playwright install
```

## Running Tests

### 🎯 Recommended: Full Orchestration

**No manual setup required! Just run:**

```bash
npm run test:full          # Run all tests
npm run test:full:ui       # Run with UI mode
npm run test:full:headed   # Run in headed mode
```

The test runner will:
1. Start PostgreSQL container (Testcontainers)
2. Run database migrations
3. Seed test data
4. Start backend server
5. Start frontend server
6. Run all tests
7. Clean up everything

**See [FULL_ORCHESTRATION.md](./FULL_ORCHESTRATION.md) for details**

---

### Standard Mode (Manual Setup)

**IMPORTANT**: Before running tests in standard mode, you need to have both the backend and frontend servers running.

### Automatic Test Data Seeding

The tests will **automatically seed the database** with test filaments if none exist. The global setup:
1. Checks if filaments exist in the database
2. If none found, automatically runs the SQL seed script
3. Tries Docker first (`docker exec ... psql`), then falls back to `psql`
4. Verifies the seeding was successful

**No manual intervention required!** Just make sure your database is running.

If automatic seeding fails, you can manually seed with:
```bash
docker exec -i uberprints-db psql -U postgres -d uberprints < test/UberPrints.Client.Playwright/seed-testdata.sql
```

### Option 1: Manual Server Start (Recommended)

In separate terminal windows:

```bash
# Terminal 1: Start backend
cd src/UberPrints.Server
dotnet run

# Terminal 2: Start frontend
cd src/UberPrints.Client
npm run dev

# Terminal 3: Run tests (will auto-seed database if needed)
cd test/UberPrints.Client.Playwright
npm test
```

### Option 2: Use the Helper Script

```bash
cd test/UberPrints.Client.Playwright
./run-tests.sh
```

This script will:
- Check if servers are already running
- Start them if needed
- Run the tests
- Clean up on exit

### Run all tests (headless)
```bash
npm test
```

### Run tests with UI mode (recommended for development)
```bash
npm run test:ui
```

### Run tests in headed mode (see the browser)
```bash
npm run test:headed
```

### Run tests in debug mode
```bash
npm run test:debug
```

### Run specific test file
```bash
npx playwright test tests/home.spec.ts
```

### Run tests in a specific browser
```bash
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit
```

### Run tests on mobile devices
```bash
npx playwright test --project="Mobile Chrome"
npx playwright test --project="Mobile Safari"
```

## Test Reports

After running tests, view the HTML report:

```bash
npm run report
```

The report will open in your browser showing:
- Test results
- Screenshots of failures
- Trace viewer for failed tests
- Performance metrics

## Test Structure

```
test/UberPrints.Client.Playwright/
├── tests/
│   ├── home.spec.ts          # Home page and navigation tests
│   ├── filaments.spec.ts     # Filaments catalog tests
│   ├── new-request.spec.ts   # Create print request tests
│   ├── requests.spec.ts      # View requests tests
│   ├── auth.spec.ts          # Authentication and guest session tests
│   ├── e2e-workflow.spec.ts  # End-to-end user journey tests
│   └── helpers.ts            # Test helper functions
├── playwright.config.ts       # Playwright configuration
├── package.json
└── README.md
```

## Test Coverage

### Home Page (`home.spec.ts`)
- ✓ Page loads successfully
- ✓ Navigation links are visible
- ✓ Navigation between pages works
- ✓ Call-to-action buttons are functional

### Filaments (`filaments.spec.ts`)
- ✓ Filaments list displays
- ✓ In-stock filtering works
- ✓ Filament properties are shown
- ✓ Responsive on mobile devices

### New Request (`new-request.spec.ts`)
- ✓ Form displays correctly
- ✓ Required field validation
- ✓ Guest session creation
- ✓ Optional fields work
- ✓ Successful request submission

### View Requests (`requests.spec.ts`)
- ✓ Requests list displays
- ✓ Empty state shown when no requests
- ✓ Request cards show details
- ✓ Clicking request shows details
- ✓ Filter and sort functionality
- ✓ Mobile responsive

### Authentication (`auth.spec.ts`)
- ✓ Login button visible
- ✓ Guest user indicator
- ✓ Dashboard access control
- ✓ Admin panel restrictions
- ✓ Guest session creation
- ✓ Session persistence
- ✓ Request tracking with guest session

### E2E Workflows (`e2e-workflow.spec.ts`)
- ✓ Complete user journey: browse → create → view
- ✓ Guest user creates and tracks request
- ✓ Navigation through all main pages
- ✓ Mobile user creates request
- ✓ Error handling for invalid input
- ✓ Keyboard navigation accessibility

## Configuration

The tests are configured in `playwright.config.ts` with:

- **Base URL**: `http://localhost:5173` (Vite dev server)
- **Browsers**: Chromium, Firefox, WebKit
- **Mobile**: Mobile Chrome (Pixel 5), Mobile Safari (iPhone 12)
- **Server Mode**: `reuseExistingServer: true` (expects servers to be running)
- **Retries**: 2 on CI, 0 locally
- **Traces**: Captured on first retry
- **Screenshots**: Captured on failure

## Web Servers

By default, the configuration expects you to manually start the servers. If you want Playwright to automatically start them:

1. Edit `playwright.config.ts`
2. Change `reuseExistingServer: true` to `reuseExistingServer: false`

**Required Servers:**

1. **Backend**: `dotnet run` from `src/UberPrints.Server`
   - URL: `https://localhost:7001`

2. **Frontend**: `npm run dev` from `src/UberPrints.Client`
   - URL: `http://localhost:5173`

**Note**: Auto-starting servers can be slow. Manual startup is recommended for faster test iterations.

## Writing New Tests

### Basic Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test('should do something', async ({ page }) => {
    await page.goto('/path');

    // Your test assertions
    await expect(page.getByRole('heading')).toBeVisible();
  });
});
```

### Using Helpers

```typescript
import { createPrintRequest, navigateAndVerify } from './helpers';

test('my test', async ({ page }) => {
  await navigateAndVerify(page, '/new-request');

  await createPrintRequest(page, {
    requesterName: 'Test User',
    modelUrl: 'https://example.com/model.stl',
    notes: 'Test notes',
  });
});
```

### Mobile-Specific Tests

```typescript
test('mobile test', async ({ page, isMobile }) => {
  test.skip(!isMobile, 'Mobile only');

  // Your mobile-specific test
});
```

## Debugging Tests

### VS Code Debugging

1. Install the Playwright VS Code extension
2. Set breakpoints in your tests
3. Click "Debug" in the test file

### Playwright Inspector

```bash
npm run test:debug
```

This opens the Playwright Inspector where you can:
- Step through tests
- Inspect selectors
- View network traffic
- See console logs

### Trace Viewer

After a test failure:

```bash
npx playwright show-trace test-results/path-to-trace.zip
```

This shows:
- Full test execution timeline
- DOM snapshots at each step
- Network requests
- Console logs

## Continuous Integration

For CI/CD pipelines, set the `CI` environment variable:

```bash
CI=true npm test
```

This will:
- Run tests in parallel (workers=1 on CI)
- Retry failed tests twice
- Generate HTML reports
- Not reuse existing servers

## Common Selectors

The tests use semantic selectors for reliability:

```typescript
// Prefer role-based selectors
page.getByRole('button', { name: /submit/i })
page.getByRole('heading', { name: /title/i })
page.getByRole('link', { name: /home/i })

// Use labels for form fields
page.getByLabel(/requester name/i)

// Use test IDs for custom components
page.locator('[data-testid="request-card"]')

// Use text for content
page.getByText(/success/i)
```

## Best Practices

1. **Use semantic selectors**: Prefer `getByRole`, `getByLabel`, `getByText` over CSS selectors
2. **Wait for network**: Use `page.waitForLoadState('networkidle')` after navigation
3. **Test user flows**: Focus on real user journeys, not implementation details
4. **Mobile testing**: Include mobile viewport tests for responsive features
5. **Accessibility**: Test keyboard navigation and screen reader compatibility
6. **Error states**: Test error handling and validation messages
7. **Independent tests**: Each test should be able to run independently
8. **Clean state**: Tests should not depend on previous test state

## Troubleshooting

### Tests are flaky
- Increase timeouts: `await page.waitForTimeout(1000)`
- Wait for specific elements: `await page.waitForSelector('[data-testid="item"]')`
- Use `networkidle`: `await page.waitForLoadState('networkidle')`

### Server won't start
- Check if ports 5173 and 7001 are available
- Verify backend and frontend build successfully
- Check `playwright.config.ts` paths are correct

### Browser not found
- Run `npx playwright install`
- Check system dependencies: `npx playwright install-deps`

### Slow tests
- Run specific test files instead of all tests
- Use `--project` to run single browser
- Disable video/trace in config for faster runs

## Documentation

- **[FULL_ORCHESTRATION.md](./FULL_ORCHESTRATION.md)** - Complete guide to full test orchestration with Testcontainers
- **[TESTING_GUIDE.md](./TESTING_GUIDE.md)** - Practical guide for writing tests with Page Objects
- **[TEST_IMPROVEMENTS.md](./TEST_IMPROVEMENTS.md)** - Technical overview of test improvements

## Resources

- [Playwright Documentation](https://playwright.dev)
- [Playwright Best Practices](https://playwright.dev/docs/best-practices)
- [Playwright Test Generator](https://playwright.dev/docs/codegen)
- [Playwright Trace Viewer](https://playwright.dev/docs/trace-viewer)
- [Testcontainers](https://testcontainers.com/)
