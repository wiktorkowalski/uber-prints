import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object for the Filaments page
 */
export class FilamentsPage extends BasePage {
  readonly heading: Locator;
  readonly inStockFilter: Locator;
  readonly filamentCards: Locator;

  constructor(page: Page) {
    super(page);

    this.heading = page.getByRole('heading', { name: /available filaments/i });
    this.inStockFilter = page.getByLabel(/in stock only/i).or(page.getByText(/show.*in stock/i));
    this.filamentCards = page.locator('[data-testid="filament-card"]')
      .or(page.locator('.filament'))
      .or(page.getByText(/PLA|ABS|PETG|TPU/i));
  }

  async goto() {
    await super.goto('/filaments');
    await this.waitForFilamentsToLoad();
  }

  /**
   * Wait for filaments page to load
   */
  async waitForFilamentsToLoad() {
    await expect(this.heading).toBeVisible();
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Get the number of visible filament cards
   */
  async getFilamentCount(): Promise<number> {
    return await this.filamentCards.count();
  }

  /**
   * Toggle the in-stock filter
   */
  async toggleInStockFilter(): Promise<boolean> {
    if (await this.inStockFilter.isVisible()) {
      await this.inStockFilter.click();
      await this.page.waitForTimeout(500);
      return true;
    }
    return false;
  }

  /**
   * Verify filament cards display required properties
   */
  async verifyFilamentProperties() {
    const count = await this.getFilamentCount();

    if (count > 0) {
      const firstFilament = this.filamentCards.first();
      await expect(firstFilament).toBeVisible();
      return true;
    }

    return false;
  }

  /**
   * Verify mobile layout
   */
  async verifyMobileLayout() {
    await expect(this.heading).toBeVisible();
  }

  /**
   * Get filament details by index
   */
  async getFilamentDetails(index: number = 0): Promise<{
    element: Locator;
    text: string;
  } | null> {
    const count = await this.getFilamentCount();

    if (index >= count) {
      return null;
    }

    const filament = this.filamentCards.nth(index);
    const text = (await filament.textContent()) || '';

    return { element: filament, text };
  }

  /**
   * Check if any filaments are displayed
   */
  async hasFilaments(): Promise<boolean> {
    return (await this.getFilamentCount()) > 0;
  }
}
