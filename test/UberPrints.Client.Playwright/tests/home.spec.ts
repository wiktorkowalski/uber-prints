import { test, expect } from '@playwright/test';

test.describe('Home Page', () => {
  test('should load home page successfully', async ({ page }) => {
    await page.goto('/');

    // Check that the page title is correct
    await expect(page).toHaveTitle(/UberPrints/);

    // Check for main heading - "Welcome to UberPrints"
    await expect(page.getByRole('heading', { name: /welcome to uberprints/i })).toBeVisible();
  });

  test('should display navigation links', async ({ page, isMobile }) => {
    await page.goto('/');

    if (!isMobile) {
      // Check navigation links (visible on desktop only)
      await expect(page.getByRole('link', { name: /^home$/i })).toBeVisible();
      await expect(page.getByRole('link', { name: /new request/i }).first()).toBeVisible();
      await expect(page.getByRole('link', { name: /all requests/i }).first()).toBeVisible();
      await expect(page.getByRole('link', { name: /filaments/i }).first()).toBeVisible();
    } else {
      // On mobile, check that main action buttons are visible
      await expect(page.getByRole('button', { name: /submit request/i })).toBeVisible();
    }
  });

  test('should navigate to different pages', async ({ page, isMobile }) => {
    test.skip(isMobile, 'Navigation test is desktop-only');

    await page.goto('/');

    // Navigate to New Request page
    await page.getByRole('link', { name: /new request/i }).first().click();
    await expect(page).toHaveURL(/.*\/request\/new/);

    // Navigate to All Requests page
    await page.goto('/');
    await page.getByRole('link', { name: /all requests/i }).first().click();
    await expect(page).toHaveURL(/.*\/requests/);

    // Navigate to Filaments page
    await page.goto('/');
    await page.getByRole('link', { name: /filaments/i }).first().click();
    await expect(page).toHaveURL(/.*\/filaments/);
  });

  test('should display call-to-action buttons', async ({ page }) => {
    await page.goto('/');

    // Check for CTA buttons on home page
    const submitRequestButton = page.getByRole('link', { name: /submit request/i });
    const viewAllRequestsButton = page.getByRole('link', { name: /view all requests/i });

    await expect(submitRequestButton).toBeVisible();
    await expect(viewAllRequestsButton).toBeVisible();

    // Verify submit button is clickable
    await submitRequestButton.click();
    await expect(page).toHaveURL(/.*\/request\/new/);
  });
});
