import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object for the Home page
 */
export class HomePage extends BasePage {
  readonly heading: Locator;
  readonly homeLink: Locator;
  readonly newRequestLink: Locator;
  readonly allRequestsLink: Locator;
  readonly filamentsLink: Locator;
  readonly submitRequestButton: Locator;
  readonly viewAllRequestsButton: Locator;
  readonly loginButton: Locator;
  readonly getStartedHeading: Locator;

  constructor(page: Page) {
    super(page);

    // Main elements
    this.heading = page.getByRole('heading', { name: /welcome to uberprints/i });

    // Navigation links (desktop)
    this.homeLink = page.getByRole('link', { name: /^home$/i });
    this.newRequestLink = page.getByRole('link', { name: /new request/i }).first();
    this.allRequestsLink = page.getByRole('link', { name: /all requests/i }).first();
    this.filamentsLink = page.getByRole('link', { name: /filaments/i }).first();

    // Call-to-action buttons
    this.submitRequestButton = page.getByRole('link', { name: /submit request/i });
    this.viewAllRequestsButton = page.getByRole('link', { name: /view all requests/i });

    // Authentication
    this.loginButton = page.getByRole('button', { name: /log.*in|sign.*in/i })
      .or(page.getByRole('link', { name: /log.*in|sign.*in/i }));
    this.getStartedHeading = page.getByRole('heading', { name: /get started/i });
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
      await expect(this.homeLink).toBeVisible();
      await expect(this.newRequestLink).toBeVisible();
      await expect(this.allRequestsLink).toBeVisible();
      await expect(this.filamentsLink).toBeVisible();
    }
  }

  async verifyCallToActionButtons() {
    await expect(this.submitRequestButton).toBeVisible();
    await expect(this.viewAllRequestsButton).toBeVisible();
  }

  async clickSubmitRequest() {
    await this.submitRequestButton.click();
    await this.page.waitForURL(/.*\/request\/new/);
  }

  async clickViewAllRequests() {
    await this.viewAllRequestsButton.click();
    await this.page.waitForURL(/.*\/requests/);
  }

  async navigateToNewRequest() {
    await this.newRequestLink.click();
    await this.page.waitForURL(/.*\/request\/new/);
  }

  async navigateToAllRequests() {
    await this.allRequestsLink.click();
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
    await expect(this.getStartedHeading).toBeVisible();
  }
}
