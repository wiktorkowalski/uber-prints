import { Page, Locator, expect } from '@playwright/test';

/**
 * Base page class with common functionality for all page objects
 */
export class BasePage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  /**
   * Navigate to a specific path and wait for page to be ready
   */
  async goto(path: string = '/') {
    await this.page.goto(path);
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Wait for an element to be visible
   */
  async waitForElement(locator: Locator, timeout: number = 5000) {
    await locator.waitFor({ state: 'visible', timeout });
  }

  /**
   * Wait for navigation to complete
   */
  async waitForNavigation() {
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Check if currently on an error page
   */
  async isErrorPage(): Promise<boolean> {
    const errorIndicators = [
      this.page.getByText(/error/i),
      this.page.getByText(/something went wrong/i),
      this.page.getByText(/not found/i),
      this.page.getByText(/404/),
      this.page.getByText(/500/),
    ];

    for (const indicator of errorIndicators) {
      const visible = await indicator.isVisible().catch(() => false);
      if (visible) return true;
    }

    return false;
  }

  /**
   * Verify page loaded successfully without errors
   */
  async verifyNoErrors() {
    const hasError = await this.isErrorPage();
    expect(hasError).toBeFalsy();
  }

  /**
   * Get current URL
   */
  getUrl(): string {
    return this.page.url();
  }

  /**
   * Check if URL contains a specific path
   */
  urlContains(path: string): boolean {
    return this.page.url().includes(path);
  }
}
