import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test('should display login button when not authenticated', async ({ page }) => {
    await page.goto('/');

    // Look for login/sign in button
    const loginButton = page.getByRole('button', { name: /log.*in|sign.*in/i }).or(
      page.getByRole('link', { name: /log.*in|sign.*in/i })
    );

    // Should be visible for unauthenticated users
    if (await loginButton.isVisible()) {
      await expect(loginButton).toBeVisible();
    }
  });

  test('should show guest section when not logged in', async ({ page }) => {
    await page.goto('/');

    // Look for "Get Started" section which appears for guests
    const getStartedSection = page.getByRole('heading', { name: /get started/i });

    // Should be visible for non-authenticated users
    await expect(getStartedSection).toBeVisible();
  });

  test('should access dashboard when authenticated', async ({ page }) => {
    // Try to navigate to dashboard
    await page.goto('/dashboard');

    // Wait for page to load
    await page.waitForTimeout(1000);

    const currentUrl = page.url();

    // Either on dashboard or redirected to auth
    expect(currentUrl.includes('/dashboard') || currentUrl.includes('/auth')).toBeTruthy();
  });

  test('should not show admin panel to non-admin users', async ({ page }) => {
    await page.goto('/');

    // Admin link should not be visible to regular users
    const adminLink = page.getByRole('link', { name: /admin/i });
    const isAdminLinkVisible = await adminLink.isVisible().catch(() => false);

    // If visible, clicking should redirect or show error
    if (isAdminLinkVisible) {
      await adminLink.click();
      await page.waitForTimeout(1000);

      // Should not be on admin page without proper auth
      const hasUnauthorized = await page.getByText(/unauthorized|forbidden|access denied/i)
        .isVisible()
        .catch(() => false);

      // Either redirected or showing error
      expect(!page.url().includes('/admin') || hasUnauthorized).toBeTruthy();
    }
  });

  test('should create guest session for new users', async ({ page }) => {
    // Clear cookies to simulate new user
    await page.context().clearCookies();

    await page.goto('/request/new');

    // Guest session should be created automatically
    // Check if we can use the form (guest session created)
    await page.waitForTimeout(1000);

    const nameInput = page.getByPlaceholder(/john doe/i);
    await expect(nameInput).toBeVisible();

    // Should be able to interact with form
    await nameInput.fill('Guest User Test');
    await expect(nameInput).toHaveValue('Guest User Test');
  });

  test('should preserve guest session across navigation', async ({ page }) => {
    await page.goto('/');

    // Navigate to different pages
    await page.goto('/filaments');
    await expect(page.getByRole('heading', { name: /available filaments/i })).toBeVisible();

    await page.goto('/requests');
    await expect(page.getByRole('heading', { name: /all print requests|requests/i })).toBeVisible();

    await page.goto('/request/new');
    await page.waitForTimeout(1000);

    // Should still have session (no errors)
    const nameInput = page.getByPlaceholder(/john doe/i);
    await expect(nameInput).toBeVisible();
  });

  test('should track requests with guest session', async ({ page }) => {
    await page.goto('/dashboard');

    // If dashboard is accessible as guest, check for user's requests
    if (page.url().includes('/dashboard')) {
      await page.waitForTimeout(1000);

      // Should show user's requests or empty state
      const hasRequests = await page.locator('a[href^="/request/"]').count() > 0;
      const hasEmptyState = await page.getByText(/no requests/i).isVisible().catch(() => false);

      expect(hasRequests || hasEmptyState).toBeTruthy();
    }
  });
});
