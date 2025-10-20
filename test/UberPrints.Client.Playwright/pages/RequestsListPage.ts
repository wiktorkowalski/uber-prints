import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object for the Requests List page
 */
export class RequestsListPage extends BasePage {
  readonly heading: Locator;
  readonly tabs: Locator;
  readonly requestCards: Locator;
  readonly filterButton: Locator;
  readonly statusFilter: Locator;

  constructor(page: Page) {
    super(page);

    this.heading = page.getByRole('heading', { name: 'All Print Requests', level: 1 });
    this.tabs = page.getByRole('tab');
    this.requestCards = page.locator('[data-testid="request-card"]')
      .or(page.locator('.request-card'))
      .or(page.locator('a[href^="/request/"]'));
    this.filterButton = page.getByRole('button', { name: /filter|sort/i });
    this.statusFilter = page.getByLabel(/status|filter by/i);
  }

  async goto() {
    await super.goto('/requests');
    await this.waitForRequestsToLoad();
  }

  /**
   * Wait for requests to load
   */
  async waitForRequestsToLoad() {
    await expect(this.heading).toBeVisible();
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Get the number of visible request cards
   */
  async getRequestCount(): Promise<number> {
    return await this.requestCards.count();
  }

  /**
   * Verify page has loaded with either tabs or request cards
   */
  async verifyPageLoaded() {
    await expect(this.heading).toBeVisible();

    const hasTabsOrCards =
      (await this.tabs.count()) > 0 || (await this.requestCards.count()) > 0;

    expect(hasTabsOrCards).toBeTruthy();
  }

  /**
   * Click on a request card by index
   */
  async clickRequestByIndex(index: number = 0) {
    const count = await this.requestCards.count();

    if (count === 0) {
      throw new Error('No request cards found');
    }

    if (index >= count) {
      throw new Error(`Request index ${index} out of range (max: ${count - 1})`);
    }

    await this.requestCards.nth(index).click();

    // Wait for navigation or details to appear
    await this.page.waitForTimeout(1000);
  }

  /**
   * Get status badge text from a specific request card
   */
  async getRequestStatus(index: number = 0): Promise<string | null> {
    const card = this.requestCards.nth(index);
    const statusBadge = card.getByText(/pending|accepted|completed|rejected|on hold/i);

    if (await statusBadge.isVisible()) {
      return await statusBadge.textContent();
    }

    return null;
  }

  /**
   * Verify request details are visible (either in modal or detail page)
   */
  async verifyRequestDetailsVisible() {
    const detailsVisible = await this.page
      .getByText(/model url|tracking token|status history/i)
      .isVisible()
      .catch(() => false);

    expect(detailsVisible).toBeTruthy();
  }

  /**
   * Apply a filter (if filter controls exist)
   */
  async applyFilter() {
    if (await this.filterButton.isVisible()) {
      await this.filterButton.click();
      await this.page.waitForTimeout(500);
      return true;
    } else if (await this.statusFilter.isVisible()) {
      await this.statusFilter.click();
      await this.page.waitForTimeout(500);
      return true;
    }

    return false;
  }

  /**
   * Verify the page is responsive on mobile
   */
  async verifyMobileLayout() {
    await expect(this.heading).toBeVisible();

    // On mobile, cards should stack vertically
    const count = await this.requestCards.count();

    if (count > 0) {
      await expect(this.requestCards.first()).toBeVisible();
    }
  }

  /**
   * Check if any requests are displayed
   */
  async hasRequests(): Promise<boolean> {
    return (await this.getRequestCount()) > 0;
  }

  /**
   * Verify empty state is shown when no requests
   */
  async verifyEmptyState() {
    const emptyMessage = this.page.getByText(/no requests/i);
    await expect(emptyMessage).toBeVisible();
  }
}
