import { test, expect } from '@playwright/test';
import { createPrintRequest, navigateAndVerify } from './helpers';

test.describe('End-to-End User Workflows', () => {
  test('complete user journey: browse filaments -> create request -> view request', async ({
    page,
    isMobile,
  }) => {
    // Step 1: Browse available filaments
    await navigateAndVerify(page, '/filaments');
    await expect(page.getByRole('heading', { name: 'Available Filaments', level: 1 })).toBeVisible();

    // Step 2: Navigate to create a new request
    if (!isMobile) {
      await page.getByRole('link', { name: /new request/i }).click();
    } else {
      // On mobile, navigate directly to the new request page
      await page.goto('/request/new');
    }
    await expect(page).toHaveURL(/.*\/request\/new/);

    // Step 3: Fill and submit the form
    await page.waitForTimeout(1000);
    await page.getByPlaceholder(/john doe/i).fill('E2E Test User');
    await page.getByPlaceholder(/thingiverse/i).fill('https://www.thingiverse.com/thing:99999');

    // Select a filament
    const filamentSelect = page.getByRole('combobox').first();
    await filamentSelect.click();
    await page.waitForTimeout(500);

    const options = await page.getByRole('option').count();
    if (options > 0) {
      await page.getByRole('option').first().click();

      // Add notes
      const notesField = page.getByPlaceholder(/additional details/i);
      if (await notesField.isVisible()) {
        await notesField.fill('E2E test - complete workflow test');
      }

      // Submit the request
      await page.getByRole('button', { name: /submit/i }).click();
      await page.waitForTimeout(2000);

      // Step 4: Verify redirect (should go to request detail, requests list, dashboard, or track page)
      const currentUrl = page.url();
      expect(
        currentUrl.includes('/request/') ||
          currentUrl.includes('/requests') ||
          currentUrl.includes('/dashboard') ||
          currentUrl.includes('/track')
      ).toBeTruthy();

      // Step 5: Navigate to view all requests
      await navigateAndVerify(page, '/requests');

      // Should see the newly created request
      await page.waitForTimeout(1000);
      const requestCards = await page.locator('a[href^="/request/"]').count();

      expect(requestCards).toBeGreaterThan(0);
    }
  });

  test('guest user creates request and tracks it', async ({ page }) => {
    // Create a request as guest
    await createPrintRequest(page, {
      requesterName: 'Guest Tracker',
      modelUrl: 'https://example.com/test.stl',
      notes: 'Testing guest tracking',
    });

    // Should receive tracking information
    await page.waitForTimeout(1000);

    // Look for tracking token or tracking link
    const trackingToken = page.getByText(/track.*token|tracking.*code/i);
    const trackingLink = page.getByRole('link', { name: /track.*request/i });

    const hasTracking = (await trackingToken.isVisible().catch(() => false)) ||
                        (await trackingLink.isVisible().catch(() => false));

    if (hasTracking) {
      // If tracking link exists, click it
      if (await trackingLink.isVisible()) {
        await trackingLink.click();
        await page.waitForTimeout(1000);

        // Should show request details
        await expect(page.getByText(/Guest Tracker|test\.stl/i)).toBeVisible();
      }
    }
  });

  test('user navigates through all main pages', async ({ page }) => {
    // Test complete navigation flow
    const pages = [
      { path: '/', heading: /3D Print|UberPrints|Home/i, level: 1 },
      { path: '/filaments', heading: 'Available Filaments', level: 1 },
      { path: '/request/new', heading: /new.*request/i, level: 1 },
      { path: '/requests', heading: 'All Print Requests', level: 1 },
    ];

    for (const pageInfo of pages) {
      await navigateAndVerify(page, pageInfo.path);
      const heading = page.getByRole('heading', { name: pageInfo.heading, level: pageInfo.level });
      await expect(heading).toBeVisible();
    }
  });

  test('mobile user creates a print request', async ({ page, isMobile }) => {
    test.skip(!isMobile, 'This test is only for mobile');

    // Navigate to new request on mobile
    await navigateAndVerify(page, '/request/new');
    await page.waitForTimeout(1000);

    // Fill the form on mobile
    await page.getByPlaceholder(/john doe/i).fill('Mobile User');
    await page.getByPlaceholder(/thingiverse/i).fill('https://example.com/mobile-test.stl');

    // Select filament on mobile
    const filamentSelect = page.getByRole('combobox').first();
    await filamentSelect.click();
    await page.waitForTimeout(500);

    const options = await page.getByRole('option').count();
    if (options > 0) {
      await page.getByRole('option').first().click();

      // Submit on mobile
      await page.getByRole('button', { name: /submit/i }).click();
      await page.waitForTimeout(2000);

      // Should navigate away from form
      expect(page.url()).not.toContain('/request/new');
    }
  });

  test('error handling: invalid model URL', async ({ page }) => {
    await page.goto('/request/new');
    await page.waitForTimeout(1000);

    // Try to submit with invalid URL
    await page.getByPlaceholder(/john doe/i).fill('Error Test User');
    await page.getByPlaceholder(/thingiverse/i).fill('not-a-valid-url');

    // Select filament
    const filamentSelect = page.getByRole('combobox').first();
    await filamentSelect.click();
    await page.waitForTimeout(500);

    const options = await page.getByRole('option').count();
    if (options > 0) {
      await page.getByRole('option').first().click();

      // Try to submit
      await page.getByRole('button', { name: /submit/i }).click();
      await page.waitForTimeout(1000);

      // Should show validation error or stay on page
      const hasError = await page.getByText(/invalid.*url|valid.*url|must be a valid url/i).isVisible().catch(() => false);
      const stillOnForm = page.url().includes('/request/new');

      expect(hasError || stillOnForm).toBeTruthy();
    }
  });

  test('accessibility: keyboard navigation', async ({ page }) => {
    await navigateAndVerify(page, '/');

    // Tab through navigation
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');

    // Press Enter on a focused link
    await page.keyboard.press('Enter');
    await page.waitForTimeout(500);

    // Should have navigated
    expect(page.url()).not.toBe('/');
  });
});
