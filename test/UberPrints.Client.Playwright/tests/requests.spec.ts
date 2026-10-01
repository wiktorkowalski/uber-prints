import { test, expect } from '../fixtures/test-fixtures';
import { REQUEST_STATUS_PATTERN } from '../pages/RequestsListPage';
import { createApiHelpers } from '../utils/api-helpers';

test.describe('View Requests', () => {
  // Unique per worker, so each test can find the request this worker seeded.
  const seededName = `E2E Seeded ${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;

  // Seed one public request so the list tests never depend on leftover data.
  test.beforeAll(async ({ playwright }) => {
    const apiContext = await playwright.request.newContext();
    try {
      const api = createApiHelpers(apiContext);
      const filament = await api.getFirstAvailableFilament();
      expect(filament, 'seed-testdata.sql should provide an in-stock filament').toBeTruthy();

      const { guestSessionToken } = await api.createGuestSession();
      await api.createRequest(
        {
          requesterName: seededName,
          modelUrl: 'https://www.printables.com/model/12345-e2e-seed',
          filamentId: filament.id,
        },
        guestSessionToken
      );
    } finally {
      await apiContext.dispose();
    }
  });

  test('should display requests list page', async ({ requestsListPage }) => {
    await requestsListPage.goto();
    await requestsListPage.verifyPageLoaded();
  });

  test('should display request cards with details', async ({ requestsListPage }) => {
    await requestsListPage.goto();

    const seededCard = requestsListPage.requestCards.filter({ hasText: seededName });
    await expect(seededCard).toHaveCount(1);
    await expect(seededCard).toBeVisible();

    // A new request starts as Pending
    const index = await requestsListPage.requestCards.evaluateAll(
      (rows, name) => rows.findIndex((row) => row.textContent?.includes(name)),
      seededName
    );
    const status = await requestsListPage.getRequestStatus(index);
    expect(status).toMatch(REQUEST_STATUS_PATTERN);
    expect(status).toMatch(/pending/i);
  });

  test('should allow clicking on request to view details', async ({ page, requestsListPage }) => {
    await requestsListPage.goto();

    await requestsListPage.requestCards.filter({ hasText: seededName }).click();

    await expect(page).toHaveURL(/\/requests\/[0-9a-f-]{36}$/);
    await expect(page.getByRole('heading', { name: seededName, level: 1 })).toBeVisible();
    await requestsListPage.verifyRequestDetailsVisible();
  });

  test('should show page heading and structure', async ({ requestsListPage }) => {
    await requestsListPage.goto();

    await expect(requestsListPage.heading).toBeVisible();
    // Filter tabs (All + stages) always render, and the seeded request gives at least one row
    await expect(requestsListPage.tabs.first()).toBeVisible();
    expect(await requestsListPage.getRequestCount()).toBeGreaterThan(0);
  });
});
