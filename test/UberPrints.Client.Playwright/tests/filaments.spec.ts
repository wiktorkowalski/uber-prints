import { test, expect } from '../fixtures/test-fixtures';

test.describe('Filaments Page', () => {
  test('should display filaments list', async ({ filamentsPage }) => {
    await filamentsPage.goto();
    await expect(filamentsPage.heading).toBeVisible();
  });

  test('should display filament properties', async ({ filamentsPage }) => {
    await filamentsPage.goto();

    const hasFilaments = await filamentsPage.verifyFilamentProperties();

    expect(hasFilaments, 'Should have at least one filament displayed').toBeTruthy();
  });

  test('should display multiple filaments if available', async ({ filamentsPage }) => {
    await filamentsPage.goto();

    const count = await filamentsPage.getFilamentCount();

    expect(count, 'Should have filaments available').toBeGreaterThan(0);

    // If multiple filaments, verify more than one is visible
    if (count > 1) {
      await expect(filamentsPage.filamentCards.nth(1)).toBeVisible();
    }
  });

  test('should load filaments data from API', async ({ filamentsPage }) => {
    await filamentsPage.goto();

    // Verify page loaded successfully
    await filamentsPage.verifyNoErrors();

    // Verify filaments are displayed
    const hasFilaments = await filamentsPage.hasFilaments();
    expect(hasFilaments).toBeTruthy();
  });
});
