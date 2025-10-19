import { test as base, expect, Page } from '@playwright/test';

/**
 * Test fixtures for UberPrints E2E tests
 * Provides seeded test data and helper functions
 */

interface TestData {
  filaments: Array<{
    name: string;
    colour: string;
    material: string;
    brand: string;
    stockAmount: number;
  }>;
}

type UberPrintsFixtures = {
  testData: TestData;
  seededPage: Page;
};

/**
 * Extended test with custom fixtures
 */
export const test = base.extend<UberPrintsFixtures>({
  // Test data fixture - provides sample filament data
  testData: async ({}, use) => {
    const data: TestData = {
      filaments: [
        {
          name: 'PLA Black',
          colour: '#000000',
          material: 'PLA',
          brand: 'Test Brand',
          stockAmount: 1000,
        },
        {
          name: 'PLA White',
          colour: '#FFFFFF',
          material: 'PLA',
          brand: 'Test Brand',
          stockAmount: 1000,
        },
        {
          name: 'PETG Blue',
          colour: '#0000FF',
          material: 'PETG',
          brand: 'Test Brand',
          stockAmount: 800,
        },
        {
          name: 'ABS Red',
          colour: '#FF0000',
          material: 'ABS',
          brand: 'Test Brand',
          stockAmount: 0,
        },
      ],
    };
    await use(data);
  },

  // Seeded page fixture - ensures database has test data before tests run
  seededPage: async ({ page, testData }, use) => {
    // Check if filaments exist in database
    await page.goto('/filaments');
    await page.waitForLoadState('networkidle');

    // Check if we see the "No filaments available" message
    const noFilamentsMessage = page.getByText(/no filaments available/i);
    const hasNoFilaments = await noFilamentsMessage.isVisible().catch(() => false);

    if (hasNoFilaments) {
      console.log('⚠️  No filaments found in database!');
      console.log('Please seed the database with test filaments:');
      console.log('  docker exec -i uberprints-db psql -U postgres -d uberprints < test/UberPrints.Client.Playwright/seed-testdata.sql');
      console.log('');
      console.log('Or manually add filaments via the admin panel.');

      // Still provide the page, but tests may fail
      // This gives a clear message about what's needed
    } else {
      // Filaments exist, all good
      console.log('✓ Filaments found in database');
    }

    await use(page);
  },
});

// Re-export expect from Playwright
export { expect };
