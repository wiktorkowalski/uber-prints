import { test, expect } from '../fixtures/test-fixtures';

test.describe('Home Page', () => {
  test('should load home page successfully', async ({ homePage }) => {
    await homePage.goto();
    await homePage.verifyHeadingVisible();
  });

  test('should display navigation links', async ({ homePage, isMobile }) => {
    await homePage.goto();
    await homePage.verifyNavigationLinks(isMobile);
  });

  test('should navigate to different pages', async ({ homePage, isMobile }) => {
    test.skip(isMobile, 'Navigation test is desktop-only');

    await homePage.goto();

    // Navigate to New Request page
    await homePage.navigateToNewRequest();

    // Navigate to All Requests page
    await homePage.goto();
    await homePage.navigateToAllRequests();

    // Navigate to Filaments page
    await homePage.goto();
    await homePage.navigateToFilaments();
  });

  test('should display call-to-action buttons', async ({ homePage }) => {
    await homePage.goto();
    await homePage.verifyCallToActionButtons();

    // Verify submit button is clickable and navigates correctly
    await homePage.clickSubmitRequest();
  });

  test('should show guest section when not logged in', async ({ homePage }) => {
    await homePage.goto();
    await homePage.verifyGuestMode();
  });
});
