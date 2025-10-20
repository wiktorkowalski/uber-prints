import { Page, expect } from '@playwright/test';
import { NewRequestPage } from '../pages/NewRequestPage';
import { BasePage } from '../pages/BasePage';

/**
 * @deprecated Use NewRequestPage.submitRequest() instead
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
  const newRequestPage = new NewRequestPage(page);
  await newRequestPage.submitRequest({
    requesterName: data.requesterName,
    modelUrl: data.modelUrl,
    notes: data.notes,
    requestDelivery: data.requestDelivery,
  });
}

/**
 * Helper function to wait for API calls to complete
 */
export async function waitForApiResponse(
  page: Page,
  urlPattern: string | RegExp,
  expectedStatus: number = 200
) {
  return page.waitForResponse(
    (response) =>
      (typeof urlPattern === 'string'
        ? response.url().includes(urlPattern)
        : urlPattern.test(response.url())) && response.status() === expectedStatus,
    { timeout: 10000 }
  );
}

/**
 * Helper function to check if user is on error page
 */
export async function isOnErrorPage(page: Page): Promise<boolean> {
  const basePage = new BasePage(page);
  return basePage.isErrorPage();
}

/**
 * Helper to navigate and verify page loaded successfully
 */
export async function navigateAndVerify(page: Page, path: string) {
  const basePage = new BasePage(page);
  await basePage.goto(path);
  await basePage.waitForNavigation();
  await basePage.verifyNoErrors();
}

/**
 * Helper to wait for element with timeout
 */
export async function waitForElement(
  page: Page,
  selector: string,
  options: { timeout?: number; state?: 'visible' | 'hidden' | 'attached' } = {}
) {
  const timeout = options.timeout || 5000;
  const state = options.state || 'visible';

  await page.locator(selector).waitFor({ state, timeout });
}

/**
 * Helper to safely check if element exists without throwing
 */
export async function elementExists(page: Page, selector: string): Promise<boolean> {
  try {
    const count = await page.locator(selector).count();
    return count > 0;
  } catch {
    return false;
  }
}

/**
 * Helper to take screenshot for debugging
 */
export async function takeDebugScreenshot(page: Page, name: string) {
  await page.screenshot({
    path: `test-results/debug-${name}-${Date.now()}.png`,
    fullPage: true,
  });
}
