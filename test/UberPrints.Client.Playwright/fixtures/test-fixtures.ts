import { test as base, expect } from '@playwright/test';
import { HomePage } from '../pages/HomePage';
import { NewRequestPage } from '../pages/NewRequestPage';
import { RequestsListPage } from '../pages/RequestsListPage';
import { FilamentsPage } from '../pages/FilamentsPage';

/**
 * Extended test fixture with Page Objects
 */
type PageFixtures = {
  homePage: HomePage;
  newRequestPage: NewRequestPage;
  requestsListPage: RequestsListPage;
  filamentsPage: FilamentsPage;
};

/**
 * Extend Playwright test with custom fixtures
 */
export const test = base.extend<PageFixtures>({
  homePage: async ({ page }, use) => {
    const homePage = new HomePage(page);
    await use(homePage);
  },

  newRequestPage: async ({ page }, use) => {
    const newRequestPage = new NewRequestPage(page);
    await use(newRequestPage);
  },

  requestsListPage: async ({ page }, use) => {
    const requestsListPage = new RequestsListPage(page);
    await use(requestsListPage);
  },

  filamentsPage: async ({ page }, use) => {
    const filamentsPage = new FilamentsPage(page);
    await use(filamentsPage);
  },
});

export { expect };
