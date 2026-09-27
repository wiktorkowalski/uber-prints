import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object for the Home page
 */
export class HomePage extends BasePage {
  readonly heading: Locator;
  readonly logoLink: Locator;
  readonly newRequestLink: Locator;
  readonly requestsLink: Locator;
  readonly printerLink: Locator;
  readonly filamentsLink: Locator;
  readonly submitRequestButton: Locator;
  readonly viewAllRequestsButton: Locator;
  readonly loginButton: Locator;
  readonly requestAPrintHeading: Locator;

  constructor(page: Page) {
    super(page);

    // Main elements: the home page has no h1, the queue panel is always rendered.
    this.heading = page.getByRole('heading', { name: /^up next$/i });

    // Navigation links (desktop navbar)
    const nav = page.getByRole('navigation').first();
    this.logoLink = nav.getByRole('link', { name: /^uberprints$/i });
    this.newRequestLink = nav.getByRole('link', { name: /^new request$/i });
    this.requestsLink = nav.getByRole('link', { name: /^requests$/i });
    this.printerLink = nav.getByRole('link', { name: /^printer$/i });
    this.filamentsLink = nav.getByRole('link', { name: /^filaments$/i });

    // Call-to-action links in the page body
    const main = page.getByRole('main');
    this.submitRequestButton = main.getByRole('link', { name: /^new request$/i });
    this.viewAllRequestsButton = main.getByRole('link', { name: /^all requests$/i });

    // Authentication
    this.loginButton = page.getByRole('button', { name: /log.*in|sign.*in/i })
      .or(page.getByRole('link', { name: /log.*in|sign.*in/i }));
    this.requestAPrintHeading = page.getByRole('heading', { name: /^request a print$/i });
  }

  async goto() {
    await super.goto('/');
    await expect(this.page).toHaveTitle(/UberPrints/);
  }

  async verifyHeadingVisible() {
    await expect(this.heading).toBeVisible();
  }

  async verifyNavigationLinks(isMobile: boolean = false) {
    if (!isMobile) {
      await expect(this.logoLink).toBeVisible();
      await expect(this.newRequestLink).toBeVisible();
      await expect(this.requestsLink).toBeVisible();
      await expect(this.printerLink).toBeVisible();
      await expect(this.filamentsLink).toBeVisible();
    }
  }

  async verifyCallToActionButtons() {
    await expect(this.submitRequestButton).toBeVisible();
    await expect(this.viewAllRequestsButton).toBeVisible();
  }

  async clickSubmitRequest() {
    await this.submitRequestButton.click();
    await this.page.waitForURL(/.*\/requests\/new/);
  }

  async clickViewAllRequests() {
    await this.viewAllRequestsButton.click();
    await this.page.waitForURL(/.*\/requests/);
  }

  async navigateToNewRequest() {
    await this.newRequestLink.click();
    await this.page.waitForURL(/.*\/requests\/new/);
  }

  async navigateToAllRequests() {
    await this.requestsLink.click();
    await this.page.waitForURL(/.*\/requests/);
  }

  async navigateToFilaments() {
    await this.filamentsLink.click();
    await this.page.waitForURL(/.*\/filaments/);
  }

  async isLoggedIn(): Promise<boolean> {
    return !(await this.loginButton.isVisible().catch(() => false));
  }

  async verifyGuestMode() {
    await expect(this.loginButton).toBeVisible();
    await expect(this.requestAPrintHeading).toBeVisible();
  }
}
