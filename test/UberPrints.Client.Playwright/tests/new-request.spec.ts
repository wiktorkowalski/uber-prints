import { test, expect } from '../fixtures/test-fixtures';
import { TestDataFactory, TestUrls } from '../fixtures/test-data';

test.describe('New Print Request', () => {
  test('should display new request form', async ({ newRequestPage }) => {
    await newRequestPage.goto();
    await newRequestPage.waitForFormReady();
    await newRequestPage.verifyFormInteractive();
  });

  test('should validate required fields', async ({ newRequestPage }) => {
    await newRequestPage.goto();

    // Try to submit empty form
    await newRequestPage.submit();

    // Verify validation prevents submission
    await newRequestPage.verifyValidationError();
  });

  test('should allow filling form fields', async ({ newRequestPage }) => {
    await newRequestPage.goto();

    const testData = TestDataFactory.createPrintRequest();

    // Fill fields
    await newRequestPage.fillRequesterName(testData.requesterName);
    await newRequestPage.fillModelUrl(testData.modelUrl);

    // Verify filaments are available
    const filamentCount = await newRequestPage.getFilamentCount();
    expect(filamentCount, 'Should have filaments available').toBeGreaterThan(0);

    // Select a filament
    await newRequestPage.selectFilament(0);
  });

  test('should allow optional notes field', async ({ newRequestPage }) => {
    await newRequestPage.goto();

    const notes = 'This is a test print request with special instructions';
    await newRequestPage.fillNotes(notes);

    // Verify notes were filled
    await expect(newRequestPage.notesInput).toHaveValue(notes);
  });

  test('should have delivery option checkbox', async ({ newRequestPage }) => {
    await newRequestPage.goto();

    // Test checking delivery
    await newRequestPage.setDelivery(true);
    await expect(newRequestPage.deliveryCheckbox).toBeChecked();

    // Test unchecking delivery
    await newRequestPage.setDelivery(false);
    await expect(newRequestPage.deliveryCheckbox).not.toBeChecked();
  });

  test('should submit request successfully with valid data', async ({ newRequestPage }) => {
    await newRequestPage.goto();

    const testData = TestDataFactory.createPrintRequest({
      requesterName: 'E2E Test User',
      modelUrl: TestUrls.validThingiverseUrl,
      notes: 'Test request created by Playwright',
    });

    await newRequestPage.submitRequest(testData);

    // Should redirect away from form
    expect(newRequestPage.urlContains('/request/new')).toBeFalsy();
  });

  test('should validate invalid URL format', async ({ newRequestPage }) => {
    await newRequestPage.goto();

    const invalidData = TestDataFactory.createInvalidPrintRequest('url');

    await newRequestPage.fillRequesterName(invalidData.requesterName!);
    await newRequestPage.fillModelUrl(invalidData.modelUrl!);

    // Try to submit with invalid URL
    const filamentCount = await newRequestPage.getFilamentCount();
    if (filamentCount > 0) {
      await newRequestPage.selectFilament(0);
      await newRequestPage.submit();

      // Should show error or stay on page
      const onFormPage = newRequestPage.urlContains('/request/new');
      expect(onFormPage, 'Should stay on form page with invalid URL').toBeTruthy();
    }
  });

  test('should handle delivery requests', async ({ newRequestPage }) => {
    await newRequestPage.goto();

    const deliveryData = TestDataFactory.createDeliveryRequest();

    await newRequestPage.submitRequest(deliveryData);

    // Should redirect after submission
    expect(newRequestPage.urlContains('/request/new')).toBeFalsy();
  });
});
