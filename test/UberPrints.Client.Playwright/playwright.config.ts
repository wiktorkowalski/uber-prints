import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration with full test orchestration
 *
 * This config uses Testcontainers to:
 * - Start PostgreSQL container
 * - Run database migrations
 * - Seed test data
 * - Start backend API server
 * - Start frontend dev server
 * - Run all tests
 * - Clean up everything
 *
 * Usage: npx playwright test (setup lives in global-setup-full.ts)
 */
export default defineConfig({
  testDir: './tests',

  /* Global setup to orchestrate entire test environment */
  globalSetup: require.resolve('./global-setup-full'),

  /* Run tests in files in parallel */
  fullyParallel: true,

  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,

  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,

  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,

  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['html'],
    ['list'],
  ],

  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: 'http://localhost:5173',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',

    /* Screenshot on failure */
    screenshot: 'only-on-failure',

    /* Video on first retry */
    video: 'retain-on-failure',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports. */
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] },
    },
  ],

  /*
   * No webServer config needed - global-setup-full.ts handles everything
   * This includes:
   * - PostgreSQL (Testcontainers)
   * - Backend API (dotnet run)
   * - Frontend (npm run dev)
   */

  /* Global timeout for each test */
  timeout: 30000,

  /* Global timeout for expect assertions */
  expect: {
    timeout: 10000,
  },
});
