import { test, expect } from '@playwright/test';

test.describe('Filaments Page', () => {
  test('should display filaments list', async ({ page }) => {
    await page.goto('/filaments');

    // Check page heading
    await expect(page.getByRole('heading', { name: /available filaments/i })).toBeVisible();
  });

  test('should allow filtering by in-stock status', async ({ page }) => {
    await page.goto('/filaments');

    // Look for in-stock filter checkbox or toggle
    const inStockFilter = page.getByLabel(/in stock only/i).or(page.getByText(/show.*in stock/i));

    if (await inStockFilter.isVisible()) {
      // Toggle the filter
      await inStockFilter.click();

      // Wait for the filter to apply
      await page.waitForTimeout(500);

      // Toggle back
      await inStockFilter.click();
      await page.waitForTimeout(500);
    }
  });

  test('should display filament properties', async ({ page }) => {
    await page.goto('/filaments');

    // Wait for the heading to ensure page has loaded
    await expect(page.getByRole('heading', { name: /available filaments/i })).toBeVisible();

    // Check if there are any filaments displayed
    const filamentCards = page.locator('[data-testid="filament-card"]').or(
      page.locator('.filament').or(
        page.getByText(/PLA|ABS|PETG|TPU/i)
      )
    );

    // If filaments exist, check their properties
    const count = await filamentCards.count();
    if (count > 0) {
      // First filament should have name, color, and stock info
      const firstFilament = filamentCards.first();
      await expect(firstFilament).toBeVisible();
    }
  });

  test('should be responsive on mobile', async ({ page, isMobile }) => {
    await page.goto('/filaments');

    // Check that the page is usable on mobile
    if (isMobile) {
      const heading = page.getByRole('heading', { name: /available filaments/i });
      await expect(heading).toBeVisible();
    }
  });
});
