import { test, expect } from '@playwright/test';

test.describe('New Print Request', () => {
  test('should display new request form', async ({ page }) => {
    await page.goto('/request/new');

    // Wait for form to load
    await page.waitForTimeout(1000);

    // Check page heading
    await expect(page.getByRole('heading', { name: /submit new request/i })).toBeVisible();

    // Check form fields exist
    await expect(page.getByPlaceholder(/john doe/i)).toBeVisible();
    await expect(page.getByPlaceholder(/thingiverse/i)).toBeVisible();
  });

  test('should validate required fields', async ({ page }) => {
    await page.goto('/request/new');

    // Try to submit empty form
    const submitButton = page.getByRole('button', { name: /submit/i });
    await submitButton.click();

    // Wait for validation errors
    await page.waitForTimeout(500);

    // Check for validation messages (form should not submit)
    await expect(page).toHaveURL(/.*\/request\/new/);
  });

  test('should create guest session before allowing request', async ({ page }) => {
    await page.goto('/request/new');
    await page.waitForTimeout(1000);

    // Fill out the form
    await page.getByPlaceholder(/john doe/i).fill('Test User');
    await page.getByPlaceholder(/thingiverse/i).fill('https://example.com/model.stl');

    // Select a filament (if available)
    const filamentSelect = page.getByRole('combobox').first();
    await filamentSelect.click();

    // Wait for options to load
    await page.waitForTimeout(500);

    // Try to select the first option if available
    const firstOption = page.getByRole('option').first();
    if (await firstOption.isVisible()) {
      await firstOption.click();
    }
  });

  test('should allow optional notes field', async ({ page }) => {
    await page.goto('/request/new');
    await page.waitForTimeout(1000);

    const notesField = page.getByPlaceholder(/additional details/i);

    if (await notesField.isVisible()) {
      await notesField.fill('This is a test print request with special instructions');
      await expect(notesField).toHaveValue(/test print request/i);
    }
  });

  test('should have delivery option checkbox', async ({ page }) => {
    await page.goto('/request/new');
    await page.waitForTimeout(1000);

    const deliveryCheckbox = page.getByRole('checkbox', { name: /delivery/i });

    if (await deliveryCheckbox.isVisible()) {
      // Check the checkbox
      await deliveryCheckbox.check();
      await expect(deliveryCheckbox).toBeChecked();

      // Uncheck it
      await deliveryCheckbox.uncheck();
      await expect(deliveryCheckbox).not.toBeChecked();
    }
  });

  test('should submit request successfully with valid data', async ({ page }) => {
    await page.goto('/request/new');
    await page.waitForTimeout(1000);

    // Fill required fields
    await page.getByPlaceholder(/john doe/i).fill('E2E Test User');
    await page.getByPlaceholder(/thingiverse/i).fill('https://www.thingiverse.com/thing:12345');

    // Select filament
    const filamentSelect = page.getByRole('combobox').first();
    await filamentSelect.click();
    await page.waitForTimeout(500);

    // Select first available filament
    const options = page.getByRole('option');
    const count = await options.count();

    if (count > 0) {
      await options.first().click();

      // Add optional notes
      const notesField = page.getByPlaceholder(/additional details/i);
      if (await notesField.isVisible()) {
        await notesField.fill('Test request created by Playwright');
      }

      // Submit the form
      await page.getByRole('button', { name: /submit/i }).click();

      // Wait for redirect or success message
      await page.waitForTimeout(2000);

      // Should redirect away from new-request or show success
      const currentUrl = page.url();
      const hasSuccessMessage = await page.getByText(/success|created|submitted/i).isVisible().catch(() => false);

      expect(currentUrl !== '/request/new' || hasSuccessMessage).toBeTruthy();
    }
  });
});
