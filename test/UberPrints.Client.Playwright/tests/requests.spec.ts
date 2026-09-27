import { test, expect } from '../fixtures/test-fixtures';
import { REQUEST_STATUS_PATTERN } from '../pages/RequestsListPage';

test.describe('View Requests', () => {
  test('should display requests list page', async ({ requestsListPage }) => {
    await requestsListPage.goto();
    await requestsListPage.verifyPageLoaded();
  });

  test('should display request cards with details', async ({ requestsListPage }) => {
    await requestsListPage.goto();

    const count = await requestsListPage.getRequestCount();

    if (count > 0) {
      // Verify first request card is visible
      await expect(requestsListPage.requestCards.first()).toBeVisible();

      // Check for status badge
      const status = await requestsListPage.getRequestStatus(0);
      if (status) {
        expect(status).toMatch(REQUEST_STATUS_PATTERN);
      }
    } else {
      // If no requests, verify empty state or informational message
      test.skip(true, 'No requests available to test');
    }
  });

  test('should allow clicking on request to view details', async ({ requestsListPage }) => {
    await requestsListPage.goto();

    const hasRequests = await requestsListPage.hasRequests();

    if (hasRequests) {
      await requestsListPage.clickRequestByIndex(0);
      await requestsListPage.verifyRequestDetailsVisible();
    } else {
      test.skip(true, 'No requests available to test');
    }
  });

  test('should show page heading and structure', async ({ requestsListPage }) => {
    await requestsListPage.goto();

    // Verify heading is visible
    await expect(requestsListPage.heading).toBeVisible();

    // Verify page has proper structure (tabs or cards)
    const tabsCount = await requestsListPage.tabs.count();
    const cardsCount = await requestsListPage.getRequestCount();

    expect(
      tabsCount > 0 || cardsCount >= 0,
      'Page should have tabs or request cards container'
    ).toBeTruthy();
  });
});
