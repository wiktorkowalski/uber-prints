/**
 * Test data factory for creating consistent test data
 */

export interface PrintRequestData {
  requesterName: string;
  modelUrl: string;
  notes?: string;
  requestDelivery?: boolean;
  filamentIndex?: number;
}

export class TestDataFactory {
  private static counter = 0;

  /**
   * Generate unique identifier for test data
   */
  private static getUniqueId(): string {
    return `${Date.now()}-${++this.counter}`;
  }

  /**
   * Create a valid print request with unique data
   */
  static createPrintRequest(overrides?: Partial<PrintRequestData>): PrintRequestData {
    const uniqueId = this.getUniqueId();

    return {
      requesterName: `Test User ${uniqueId}`,
      modelUrl: `https://www.thingiverse.com/thing:${uniqueId}`,
      notes: `Test request created at ${new Date().toISOString()}`,
      requestDelivery: false,
      filamentIndex: 0,
      ...overrides,
    };
  }

  /**
   * Create multiple print requests
   */
  static createMultiplePrintRequests(count: number): PrintRequestData[] {
    return Array.from({ length: count }, (_, i) =>
      this.createPrintRequest({
        requesterName: `Test User ${i + 1}`,
        modelUrl: `https://www.thingiverse.com/thing:${1000 + i}`,
        notes: `Test request #${i + 1}`,
      })
    );
  }

  /**
   * Create a print request with invalid data for validation testing
   */
  static createInvalidPrintRequest(invalidField: 'url' | 'name'): Partial<PrintRequestData> {
    const base = this.createPrintRequest();

    if (invalidField === 'url') {
      return {
        ...base,
        modelUrl: 'not-a-valid-url',
      };
    }

    if (invalidField === 'name') {
      return {
        ...base,
        requesterName: '',
      };
    }

    return base;
  }

  /**
   * Create a print request with delivery
   */
  static createDeliveryRequest(): PrintRequestData {
    return this.createPrintRequest({
      requestDelivery: true,
      notes: 'Please deliver to main office',
    });
  }

  /**
   * Create a minimal print request (only required fields)
   */
  static createMinimalRequest(): PrintRequestData {
    const uniqueId = this.getUniqueId();

    return {
      requesterName: `Minimal User ${uniqueId}`,
      modelUrl: `https://example.com/model-${uniqueId}.stl`,
    };
  }
}

/**
 * Common test URLs
 */
export const TestUrls = {
  validStl: 'https://www.thingiverse.com/thing:12345',
  validThingiverseUrl: 'https://www.thingiverse.com/thing:67890',
  validExampleUrl: 'https://example.com/model.stl',
  invalidUrl: 'not-a-valid-url',
  invalidProtocol: 'ftp://example.com/model.stl',
};

/**
 * Common test user data
 */
export const TestUsers = {
  guest: {
    name: 'Guest User',
  },
  regular: {
    name: 'Regular User',
    email: 'user@example.com',
  },
  admin: {
    name: 'Admin User',
    email: 'admin@example.com',
  },
};
