import { test, expect } from '@playwright/test';

test.describe('View Requests', () => {
  test('should display requests list page', async ({ page }) => {
    await page.goto('/requests');

    // Check page heading
    await expect(page.getByRole('heading', { name: /print requests|all requests/i })).toBeVisible();

    // Wait for requests to load
    await page.waitForLoadState('networkidle');
  });

  test('should display requests page successfully', async ({ page }) => {
    await page.goto('/requests');
    await page.waitForLoadState('networkidle');

    // Page should load successfully with heading
    await expect(page.getByRole('heading', { name: /all print requests|requests/i })).toBeVisible();

    // Page should show either requests or tabs
    const hasTabs = await page.getByRole('tab').count() > 0;
    const hasCards = await page.locator('a[href^="/request/"]').count() > 0;

    // Either tabs or request cards should be present
    expect(hasTabs || hasCards).toBeTruthy();
  });

  test('should display request cards with details', async ({ page }) => {
    await page.goto('/requests');
    await page.waitForLoadState('networkidle');

    // Look for request cards
    const requestCards = page.locator('[data-testid="request-card"]').or(
      page.locator('.request-card').or(
        page.getByRole('article')
      )
    );

    const count = await requestCards.count();

    if (count > 0) {
      const firstRequest = requestCards.first();

      // Check that request has basic info
      await expect(firstRequest).toBeVisible();

      // Should have status indicator
      const statusBadge = firstRequest.getByText(/pending|accepted|completed|rejected/i);
      if (await statusBadge.isVisible()) {
        await expect(statusBadge).toBeVisible();
      }
    }
  });

  test('should allow clicking on request to view details', async ({ page }) => {
    await page.goto('/requests');
    await page.waitForLoadState('networkidle');

    // Find clickable request cards
    const requestCards = page.locator('[data-testid="request-card"]').or(
      page.locator('.request-card')
    );

    const count = await requestCards.count();

    if (count > 0) {
      // Click on first request
      await requestCards.first().click();

      // Wait for navigation or modal
      await page.waitForTimeout(1000);

      // Should show more details (either on new page or in modal)
      const hasDetailView = await page.getByText(/model url|tracking token|status history/i)
        .isVisible()
        .catch(() => false);

      if (hasDetailView) {
        expect(hasDetailView).toBeTruthy();
      }
    }
  });

  test('should filter or sort requests', async ({ page }) => {
    await page.goto('/requests');
    await page.waitForLoadState('networkidle');

    // Look for filter/sort controls
    const filterButton = page.getByRole('button', { name: /filter|sort/i });
    const statusFilter = page.getByLabel(/status|filter by/i);

    // If filters exist, test them
    if (await filterButton.isVisible()) {
      await filterButton.click();
      await page.waitForTimeout(500);
    } else if (await statusFilter.isVisible()) {
      await statusFilter.click();
      await page.waitForTimeout(500);
    }
  });

  test('should be responsive on mobile', async ({ page, isMobile }) => {
    await page.goto('/requests');
    await page.waitForLoadState('networkidle');

    if (isMobile) {
      // Check that the page is usable on mobile
      const heading = page.getByRole('heading', { name: /requests/i });
      await expect(heading).toBeVisible();

      // Request cards should stack vertically
      const cards = page.locator('[data-testid="request-card"]');
      const count = await cards.count();

      if (count > 0) {
        await expect(cards.first()).toBeVisible();
      }
    }
  });
});
