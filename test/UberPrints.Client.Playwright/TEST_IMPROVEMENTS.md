# Playwright Test Improvements

This document describes the improvements made to the Playwright test suite for UberPrints.

## Overview

The test suite has been significantly refactored to improve maintainability, reliability, and developer experience. The improvements follow industry best practices for end-to-end testing.

## Key Improvements

### 1. Page Object Model (POM) Pattern

**Before**: Tests directly interacted with page elements using selectors scattered throughout test files.

**After**: Organized page interactions into reusable Page Object classes.

**Benefits**:
- Centralized element selectors and page interactions
- Easier maintenance when UI changes
- Better code reuse across tests
- Type-safe page interactions with TypeScript

**Structure**:
```
pages/
├── BasePage.ts           # Base class with common functionality
├── HomePage.ts           # Home page interactions
├── NewRequestPage.ts     # Print request form
├── RequestsListPage.ts   # Requests list page
├── FilamentsPage.ts      # Filaments catalog page
└── index.ts              # Exports all page objects
```

**Example Usage**:
```typescript
// Before
await page.goto('/request/new');
await page.waitForTimeout(1000);
await page.getByPlaceholder(/john doe/i).fill('Test User');

// After
await newRequestPage.goto();
await newRequestPage.fillRequesterName('Test User');
```

### 2. Test Fixtures

**Before**: Page objects were manually instantiated in each test.

**After**: Automatic dependency injection using Playwright fixtures.

**Benefits**:
- Automatic setup and teardown
- Consistent test initialization
- Cleaner test code
- Easy to extend with new fixtures

**Example**:
```typescript
// tests/home.spec.ts
import { test, expect } from '../fixtures/test-fixtures';

test('should load home page', async ({ homePage }) => {
  await homePage.goto();
  await homePage.verifyHeadingVisible();
});
```

### 3. Test Data Factory

**Before**: Test data was hardcoded or inconsistently generated.

**After**: Centralized test data generation with factories.

**Benefits**:
- Consistent test data across tests
- Unique identifiers prevent data conflicts
- Easy to create valid and invalid test cases
- Reusable test data patterns

**Example**:
```typescript
// Create valid request
const testData = TestDataFactory.createPrintRequest({
  requesterName: 'E2E Test User',
  modelUrl: TestUrls.validThingiverseUrl,
});

// Create invalid request for validation testing
const invalidData = TestDataFactory.createInvalidPrintRequest('url');
```

### 4. Removed Arbitrary Timeouts

**Before**: Heavy use of `page.waitForTimeout()` throughout tests.

**After**: Smart waiting using Playwright's built-in wait mechanisms.

**Benefits**:
- Faster test execution
- More reliable tests
- Better error messages when waits fail
- Proper waiting for actual conditions

**Examples**:
```typescript
// Before
await page.waitForTimeout(1000);

// After - wait for specific element
await expect(newRequestPage.heading).toBeVisible();

// After - wait for navigation
await page.waitForLoadState('domcontentloaded');

// After - wait for network idle
await page.waitForLoadState('networkidle');
```

### 5. API Helper Utilities

**Before**: No programmatic API interaction for test setup/cleanup.

**After**: Comprehensive API helper class for backend interactions.

**Benefits**:
- Direct API testing capability
- Setup test data programmatically
- Health checks and wait utilities
- Cleanup after tests

**Example**:
```typescript
const apiHelpers = new ApiHelpers(apiContext);

// Wait for API to be ready
await apiHelpers.waitForApi();

// Get test data
const filaments = await apiHelpers.getInStockFilaments();
const firstFilament = await apiHelpers.getFirstAvailableFilament();

// Create request via API
const request = await apiHelpers.createRequest({
  requesterName: 'Test User',
  modelUrl: 'https://example.com/model.stl',
  filamentId: firstFilament.id,
});
```

### 6. Better Assertions with Error Messages

**Before**: Generic assertions without context.

**After**: Descriptive assertions with custom error messages.

**Example**:
```typescript
// Before
expect(count > 0).toBeTruthy();

// After
expect(count, 'Should have filaments available').toBeGreaterThan(0);
```

### 7. Improved Test Organization

**Test Files**:
- `home.spec.ts` - Home page functionality
- `new-request.spec.ts` - Print request form
- `requests.spec.ts` - Requests list page
- `filaments.spec.ts` - Filaments catalog
- `auth.spec.ts` - Authentication flows
- `e2e-workflow.spec.ts` - Complete user journeys

**Support Files**:
- `fixtures/` - Test fixtures and data factories
- `pages/` - Page Object Models
- `utils/` - API helpers and utilities
- `helpers.ts` - Legacy helper functions (being phased out)

### 8. Enhanced Helper Functions

**Before**: Basic helper functions with minimal error handling.

**After**: Robust helpers with proper error handling and TypeScript types.

**New Utilities**:
- `waitForElement()` - Wait for element with options
- `elementExists()` - Safely check element existence
- `takeDebugScreenshot()` - Capture screenshots for debugging
- `navigateAndVerify()` - Navigate with error checking

## Running the Tests

### Prerequisites
```bash
cd test/UberPrints.Client.Playwright
npm install
npx playwright install
```

### Run All Tests
```bash
npm test
```

### Run Specific Browser
```bash
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit
```

### Run Specific Test File
```bash
npx playwright test tests/new-request.spec.ts
```

### Debug Mode
```bash
npm run test:debug
```

### UI Mode (Interactive)
```bash
npm run test:ui
```

### View Test Report
```bash
npm run report
```

## Test Patterns

### Pattern 1: Simple Page Verification
```typescript
test('should display page', async ({ homePage }) => {
  await homePage.goto();
  await homePage.verifyHeadingVisible();
});
```

### Pattern 2: Form Submission
```typescript
test('should submit form', async ({ newRequestPage }) => {
  await newRequestPage.goto();

  const testData = TestDataFactory.createPrintRequest();
  await newRequestPage.submitRequest(testData);

  expect(newRequestPage.urlContains('/request/new')).toBeFalsy();
});
```

### Pattern 3: Conditional Testing
```typescript
test('should apply filter if available', async ({ requestsListPage }) => {
  await requestsListPage.goto();

  const hasFilters = await requestsListPage.applyFilter();

  if (hasFilters) {
    expect(hasFilters).toBeTruthy();
  } else {
    test.skip(true, 'No filter controls available');
  }
});
```

### Pattern 4: Mobile-Specific Tests
```typescript
test('should be responsive', async ({ requestsListPage, isMobile }) => {
  test.skip(!isMobile, 'Mobile-only test');

  await requestsListPage.goto();
  await requestsListPage.verifyMobileLayout();
});
```

## Best Practices

### 1. Use Page Objects
✅ **Good**: `await newRequestPage.fillRequesterName('Test User')`
❌ **Bad**: `await page.getByPlaceholder(/john doe/i).fill('Test User')`

### 2. Use Test Data Factory
✅ **Good**: `const data = TestDataFactory.createPrintRequest()`
❌ **Bad**: Hardcoding test data in each test

### 3. Avoid Arbitrary Timeouts
✅ **Good**: `await expect(element).toBeVisible()`
❌ **Bad**: `await page.waitForTimeout(1000)`

### 4. Use Descriptive Assertions
✅ **Good**: `expect(count, 'Should have results').toBeGreaterThan(0)`
❌ **Bad**: `expect(count > 0).toBeTruthy()`

### 5. Handle Optional Features
```typescript
// Check if feature exists before testing
if (await element.isVisible()) {
  // Test the feature
} else {
  test.skip(true, 'Feature not available');
}
```

## Migration Guide

### For Existing Tests

1. **Import from fixtures**:
   ```typescript
   // Old
   import { test, expect } from '@playwright/test';

   // New
   import { test, expect } from '../fixtures/test-fixtures';
   ```

2. **Use Page Objects**:
   ```typescript
   // Old
   test('my test', async ({ page }) => {
     await page.goto('/');
   });

   // New
   test('my test', async ({ homePage }) => {
     await homePage.goto();
   });
   ```

3. **Use Test Data Factory**:
   ```typescript
   // Old
   const data = {
     requesterName: 'Test User',
     modelUrl: 'https://example.com/model.stl',
   };

   // New
   const data = TestDataFactory.createPrintRequest({
     requesterName: 'Test User',
   });
   ```

## Troubleshooting

### Tests Timing Out
- Check if backend is running on `http://localhost:5203`
- Check if frontend is running on `http://localhost:5173`
- Increase timeout in `playwright.config.ts` if needed

### Element Not Found
- Use `test:ui` mode to debug interactively
- Take screenshots: `await takeDebugScreenshot(page, 'debug')`
- Check page objects for correct selectors

### Flaky Tests
- Remove `waitForTimeout()` calls
- Use proper waiting mechanisms
- Check for race conditions in page loads

## Future Improvements

- [ ] Add visual regression testing with Percy or similar
- [ ] Add API contract testing
- [ ] Add performance testing
- [ ] Add accessibility testing with axe-core
- [ ] Add test coverage reporting
- [ ] Add parallel execution optimization
- [ ] Add test data cleanup utilities

## Resources

- [Playwright Documentation](https://playwright.dev)
- [Page Object Model Pattern](https://playwright.dev/docs/pom)
- [Playwright Best Practices](https://playwright.dev/docs/best-practices)
- [Test Fixtures](https://playwright.dev/docs/test-fixtures)
