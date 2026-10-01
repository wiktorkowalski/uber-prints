# UberPrints E2E Tests with Playwright

End-to-end tests for the UberPrints client application using Playwright.

## How it runs

`npm test` uses `playwright.config.ts`, whose `global-setup-full.ts` does everything:

1. Starts a throwaway PostgreSQL container (Testcontainers)
2. Runs migrations and seeds `seed-testdata.sql`
3. Starts the backend (`dotnet run --no-build`, Development) on port 5203 with safe overrides: empty Discord bot token, empty thermal printer URL, PrusaLink and camera pointed at localhost, test JWT/Discord OAuth values. No real DMs, tickets or printer calls, and no `.env` needed.
4. Starts the Vite dev server on port 5173
5. Runs the tests, then stops everything (backend log: `e2e-backend.log`, gitignored)

## Prerequisites

- Node.js 18+
- Docker (for Testcontainers)
- .NET SDK; run `dotnet build` from the repo root first (setup uses `--no-build`)

If Testcontainers fails with "No host port found for host IP", run with `TESTCONTAINERS_RYUK_DISABLED=true`.

## Installation

```bash
cd test/UberPrints.Client.Playwright
npm install
npx playwright install
```

## Running Tests

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

`playwright.config.ts`:

- **Base URL**: `http://localhost:5173` (Vite dev server, proxies `/api` to the backend on 5203)
- **Browsers**: Chromium, Firefox, WebKit, plus Mobile Chrome (Pixel 5) and Mobile Safari (iPhone 12)
- **Retries**: 2 on CI, 0 locally
- **Traces**: captured on first retry; **screenshots** on failure

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
- Check if ports 5173 and 5203 are available (setup frees them by killing only node/dotnet processes)
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
