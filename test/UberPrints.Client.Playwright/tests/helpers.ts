import { Page, expect } from '@playwright/test';

/**
 * Helper function to create a test print request
 */
export async function createPrintRequest(
  page: Page,
  data: {
    requesterName: string;
    modelUrl: string;
    notes?: string;
    requestDelivery?: boolean;
  }
) {
  await page.goto('/request/new');

  // Wait for form to load
  await page.waitForTimeout(1000);

  // Fill required fields using placeholders
  await page.getByPlaceholder(/john doe/i).fill(data.requesterName);
  await page.getByPlaceholder(/thingiverse/i).fill(data.modelUrl);

  // Select first available filament
  const filamentSelect = page.getByRole('combobox').first();
  await filamentSelect.click();
  await page.waitForTimeout(500);

  const firstOption = page.getByRole('option').first();
  if (await firstOption.isVisible()) {
    await firstOption.click();
  }

  // Fill optional fields
  if (data.notes) {
    const notesField = page.getByPlaceholder(/additional details/i);
    if (await notesField.isVisible()) {
      await notesField.fill(data.notes);
    }
  }

  if (data.requestDelivery) {
    const deliveryCheckbox = page.getByRole('checkbox', { name: /delivery/i });
    if (await deliveryCheckbox.isVisible()) {
      await deliveryCheckbox.check();
    }
  }

  // Submit form
  await page.getByRole('button', { name: /submit/i }).click();
  await page.waitForTimeout(2000);
}

/**
 * Helper function to wait for API calls to complete
 */
export async function waitForApiResponse(page: Page, urlPattern: string | RegExp) {
  return page.waitForResponse(
    (response) =>
      (typeof urlPattern === 'string'
        ? response.url().includes(urlPattern)
        : urlPattern.test(response.url())) && response.status() === 200
  );
}

/**
 * Helper function to check if user is on error page
 */
export async function isOnErrorPage(page: Page): Promise<boolean> {
  const errorTexts = [
    /error/i,
    /something went wrong/i,
    /not found/i,
    /404/i,
    /500/i,
  ];

  for (const pattern of errorTexts) {
    if (await page.getByText(pattern).isVisible().catch(() => false)) {
      return true;
    }
  }

  return false;
}

/**
 * Helper to create a guest session explicitly
 */
export async function createGuestSession(page: Page) {
  // Make API call to create guest session
  const response = await page.request.post('/api/auth/guest');
  const data = await response.json();
  return data.guestSessionToken;
}

/**
 * Helper to navigate and verify page loaded successfully
 */
export async function navigateAndVerify(page: Page, path: string) {
  await page.goto(path);
  await page.waitForLoadState('networkidle');

  // Verify no error occurred
  const hasError = await isOnErrorPage(page);
  expect(hasError).toBeFalsy();
}
