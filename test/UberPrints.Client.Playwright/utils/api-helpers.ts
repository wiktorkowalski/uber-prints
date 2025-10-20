import { APIRequestContext, expect } from '@playwright/test';

/**
 * API helper utilities for interacting with the backend during tests
 */
export class ApiHelpers {
  private apiContext: APIRequestContext;
  private baseUrl: string;

  constructor(apiContext: APIRequestContext, baseUrl: string = 'http://localhost:5203') {
    this.apiContext = apiContext;
    this.baseUrl = baseUrl;
  }

  /**
   * Create a guest session
   */
  async createGuestSession(): Promise<{ guestSessionToken: string; userId: string }> {
    const response = await this.apiContext.post(`${this.baseUrl}/api/auth/guest`);
    expect(response.ok()).toBeTruthy();

    const data = await response.json();
    expect(data.guestSessionToken).toBeTruthy();
    expect(data.userId).toBeTruthy();

    return data;
  }

  /**
   * Get all filaments
   */
  async getFilaments(): Promise<any[]> {
    const response = await this.apiContext.get(`${this.baseUrl}/api/filaments`);
    expect(response.ok()).toBeTruthy();

    return await response.json();
  }

  /**
   * Get a specific filament by ID
   */
  async getFilament(id: string): Promise<any> {
    const response = await this.apiContext.get(`${this.baseUrl}/api/filaments/${id}`);
    expect(response.ok()).toBeTruthy();

    return await response.json();
  }

  /**
   * Get all print requests
   */
  async getRequests(): Promise<any[]> {
    const response = await this.apiContext.get(`${this.baseUrl}/api/requests`);
    expect(response.ok()).toBeTruthy();

    return await response.json();
  }

  /**
   * Get a specific request by ID
   */
  async getRequest(id: string): Promise<any> {
    const response = await this.apiContext.get(`${this.baseUrl}/api/requests/${id}`);
    expect(response.ok()).toBeTruthy();

    return await response.json();
  }

  /**
   * Create a print request via API
   */
  async createRequest(data: {
    requesterName: string;
    modelUrl: string;
    filamentId: string;
    notes?: string;
    requestDelivery?: boolean;
  }): Promise<any> {
    const response = await this.apiContext.post(`${this.baseUrl}/api/requests`, {
      data: {
        requesterName: data.requesterName,
        modelUrl: data.modelUrl,
        filamentId: data.filamentId,
        notes: data.notes || '',
        requestDelivery: data.requestDelivery || false,
      },
    });

    expect(response.ok()).toBeTruthy();
    return await response.json();
  }

  /**
   * Track a request using guest tracking token
   */
  async trackRequest(token: string): Promise<any> {
    const response = await this.apiContext.get(`${this.baseUrl}/api/requests/track/${token}`);
    expect(response.ok()).toBeTruthy();

    return await response.json();
  }

  /**
   * Delete a request (admin only - for cleanup)
   */
  async deleteRequest(id: string, authToken?: string): Promise<void> {
    const headers = authToken ? { Authorization: `Bearer ${authToken}` } : {};

    const response = await this.apiContext.delete(`${this.baseUrl}/api/admin/requests/${id}`, {
      headers,
    });

    // Don't fail if not authorized - just for cleanup
    if (response.status() !== 401 && response.status() !== 403) {
      expect(response.ok()).toBeTruthy();
    }
  }

  /**
   * Health check - verify API is accessible
   */
  async healthCheck(): Promise<boolean> {
    try {
      const response = await this.apiContext.get(`${this.baseUrl}/api/filaments`);
      return response.ok();
    } catch {
      return false;
    }
  }

  /**
   * Wait for API to be ready
   */
  async waitForApi(maxAttempts: number = 30, delayMs: number = 1000): Promise<void> {
    for (let i = 0; i < maxAttempts; i++) {
      const isHealthy = await this.healthCheck();

      if (isHealthy) {
        return;
      }

      await new Promise((resolve) => setTimeout(resolve, delayMs));
    }

    throw new Error(`API not ready after ${maxAttempts} attempts`);
  }

  /**
   * Get in-stock filaments
   */
  async getInStockFilaments(): Promise<any[]> {
    const filaments = await this.getFilaments();
    return filaments.filter((f) => f.inStock);
  }

  /**
   * Get first available filament (useful for test data)
   */
  async getFirstAvailableFilament(): Promise<any | null> {
    const filaments = await this.getInStockFilaments();
    return filaments.length > 0 ? filaments[0] : null;
  }
}

/**
 * Create API helpers instance
 */
export function createApiHelpers(apiContext: APIRequestContext): ApiHelpers {
  return new ApiHelpers(apiContext);
}
