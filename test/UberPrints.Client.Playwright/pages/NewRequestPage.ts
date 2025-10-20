import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object for the New Request form page
 */
export class NewRequestPage extends BasePage {
  readonly heading: Locator;
  readonly requesterNameInput: Locator;
  readonly modelUrlInput: Locator;
  readonly filamentSelect: Locator;
  readonly notesInput: Locator;
  readonly deliveryCheckbox: Locator;
  readonly submitButton: Locator;

  constructor(page: Page) {
    super(page);

    this.heading = page.getByRole('heading', { name: /submit new request/i });
    this.requesterNameInput = page.getByPlaceholder(/john doe/i);
    this.modelUrlInput = page.getByPlaceholder(/thingiverse/i);
    this.filamentSelect = page.getByRole('combobox').first();
    this.notesInput = page.getByPlaceholder(/special instructions|requirements/i);
    this.deliveryCheckbox = page.getByRole('checkbox', { name: /delivery/i });
    this.submitButton = page.getByRole('button', { name: /submit/i });
  }

  async goto() {
    await super.goto('/request/new');
    await this.waitForFormReady();
  }

  /**
   * Wait for form to be fully loaded and interactive
   */
  async waitForFormReady() {
    await expect(this.heading).toBeVisible();
    await expect(this.requesterNameInput).toBeVisible();
    await expect(this.modelUrlInput).toBeVisible();
  }

  /**
   * Fill the requester name field
   */
  async fillRequesterName(name: string) {
    await this.requesterNameInput.fill(name);
  }

  /**
   * Fill the model URL field
   */
  async fillModelUrl(url: string) {
    await this.modelUrlInput.fill(url);
  }

  /**
   * Select a filament by index (0-based)
   */
  async selectFilament(index: number = 0) {
    await this.filamentSelect.click();

    // Wait for options to appear
    const options = this.page.getByRole('option');
    await options.first().waitFor({ state: 'visible' });

    const optionCount = await options.count();
    if (optionCount === 0) {
      throw new Error('No filament options available');
    }

    if (index >= optionCount) {
      throw new Error(`Filament index ${index} out of range (max: ${optionCount - 1})`);
    }

    await options.nth(index).click();
  }

  /**
   * Select a filament by name
   */
  async selectFilamentByName(name: string) {
    await this.filamentSelect.click();

    const option = this.page.getByRole('option', { name: new RegExp(name, 'i') });
    await option.waitFor({ state: 'visible' });
    await option.click();
  }

  /**
   * Fill the optional notes field
   */
  async fillNotes(notes: string) {
    if (await this.notesInput.isVisible()) {
      await this.notesInput.fill(notes);
    }
  }

  /**
   * Toggle the delivery checkbox
   */
  async setDelivery(requestDelivery: boolean) {
    if (await this.deliveryCheckbox.isVisible()) {
      if (requestDelivery) {
        await this.deliveryCheckbox.check();
      } else {
        await this.deliveryCheckbox.uncheck();
      }
    }
  }

  /**
   * Submit the form
   */
  async submit() {
    await this.submitButton.click();
  }

  /**
   * Fill and submit the complete form
   */
  async submitRequest(data: {
    requesterName: string;
    modelUrl: string;
    filamentIndex?: number;
    notes?: string;
    requestDelivery?: boolean;
  }) {
    await this.fillRequesterName(data.requesterName);
    await this.fillModelUrl(data.modelUrl);
    await this.selectFilament(data.filamentIndex ?? 0);

    if (data.notes) {
      await this.fillNotes(data.notes);
    }

    if (data.requestDelivery !== undefined) {
      await this.setDelivery(data.requestDelivery);
    }

    await this.submit();

    // Wait for navigation or response
    await this.page.waitForURL((url) => !url.pathname.includes('/request/new'), {
      timeout: 5000,
    }).catch(() => {
      // May stay on page if validation fails
    });
  }

  /**
   * Verify validation error message appears
   */
  async verifyValidationError(message?: string | RegExp) {
    // Wait a bit for validation to trigger
    await this.page.waitForTimeout(500);

    if (message) {
      const errorText = this.page.getByText(message);
      await expect(errorText).toBeVisible();
    }

    // Should still be on the form page
    expect(this.page.url()).toContain('/request/new');
  }

  /**
   * Verify the form can accept input (not disabled)
   */
  async verifyFormInteractive() {
    await expect(this.requesterNameInput).toBeEnabled();
    await expect(this.modelUrlInput).toBeEnabled();
    await expect(this.submitButton).toBeEnabled();
  }

  /**
   * Get the number of available filament options
   */
  async getFilamentCount(): Promise<number> {
    await this.filamentSelect.click();
    const options = this.page.getByRole('option');
    await options.first().waitFor({ state: 'visible', timeout: 5000 });
    const count = await options.count();

    // Close the dropdown by pressing Escape
    await this.page.keyboard.press('Escape');

    return count;
  }
}
