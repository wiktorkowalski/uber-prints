import axios, { AxiosInstance } from 'axios';
import {
  PrintRequestDto,
  CreatePrintRequestDto,
  UpdatePrintRequestDto,
  UpdatePrintRequestAdminDto,
  FilamentDto,
  UserDto,
  ChangeStatusDto,
  CreateFilamentDto,
  UpdateFilamentDto,
  UpdateStockDto,
  GuestSessionResponse,
  FilamentRequestDto,
  CreateFilamentRequestDto,
  ChangeFilamentRequestStatusDto,
} from '../types/api';

class ApiClient {
  private client: AxiosInstance;
  private baseURL: string;

  constructor() {
    // Use VITE_API_BASE_URL if set, otherwise empty string for same-origin
    // In development with Vite proxy, the empty string works fine
    this.baseURL = import.meta.env.VITE_API_BASE_URL || '';

    this.client = axios.create({
      baseURL: this.baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
      withCredentials: true,
    });

    // Add token to requests if available
    this.client.interceptors.request.use((config) => {
      const token = this.getToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      } else {
        // If no JWT token, send guest session token as custom header
        const guestToken = this.getGuestSessionToken();
        if (guestToken) {
          config.headers['X-Guest-Session-Token'] = guestToken;
        }
      }
      return config;
    });

    // Handle HTTP error responses
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        const status = error.response?.status;

        // Handle different error types
        switch (status) {
          case 401:
            // Unauthorized - clear auth and dispatch event
            this.clearToken();
            this.clearGuestSessionToken();
            window.dispatchEvent(new CustomEvent('auth:unauthorized'));
            break;

          case 403:
            // Forbidden - user doesn't have permission
            window.dispatchEvent(new CustomEvent('auth:forbidden', {
              detail: { message: error.response?.data?.message || 'Access denied' }
            }));
            break;

          case 404:
            // Not found - could be a deleted resource
            break;

          case 500:
          case 502:
          case 503:
            // Server errors - could implement retry logic here
            window.dispatchEvent(new CustomEvent('api:server-error', {
              detail: { status, message: 'Server error. Please try again later.' }
            }));
            break;

          default:
            // Network error or other issues
            if (!error.response) {
              window.dispatchEvent(new CustomEvent('api:network-error', {
                detail: { message: 'Network error. Please check your connection.' }
              }));
            }
        }

        return Promise.reject(error);
      }
    );
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  setToken(token: string): void {
    localStorage.setItem('token', token);
  }

  clearToken(): void {
    localStorage.removeItem('token');
  }

  getGuestSessionToken(): string | null {
    return localStorage.getItem('guestSessionToken');
  }

  setGuestSessionToken(token: string): void {
    localStorage.setItem('guestSessionToken', token);
  }

  clearGuestSessionToken(): void {
    localStorage.removeItem('guestSessionToken');
  }

  // Auth endpoints
  async getCurrentUser(): Promise<UserDto> {
    const response = await this.client.get<UserDto>('/api/auth/me');
    return response.data;
  }

  async logout(): Promise<void> {
    await this.client.post('/api/auth/logout');
    this.clearToken();
  }

  async createGuestSession(): Promise<GuestSessionResponse> {
    const response = await this.client.post<GuestSessionResponse>('/api/auth/guest');
    return response.data;
  }

  getDiscordLoginUrl(): string {
    const guestToken = this.getGuestSessionToken();
    const params = new URLSearchParams();
    if (guestToken) params.append('guestSessionToken', guestToken);
    return `${this.baseURL}/api/auth/login${params.toString() ? '?' + params.toString() : ''}`;
  }

  // Request endpoints
  async getRequests(): Promise<PrintRequestDto[]> {
    const response = await this.client.get<PrintRequestDto[]>('/api/requests');
    return response.data;
  }

  async getRequest(id: string): Promise<PrintRequestDto> {
    const response = await this.client.get<PrintRequestDto>(`/api/requests/${id}`);
    return response.data;
  }

  async trackRequest(token: string): Promise<PrintRequestDto> {
    const response = await this.client.get<PrintRequestDto>(`/api/requests/track/${token}`);
    return response.data;
  }

  async createRequest(data: CreatePrintRequestDto): Promise<PrintRequestDto> {
    const response = await this.client.post<PrintRequestDto>('/api/requests', data);
    return response.data;
  }

  async updateRequest(id: string, data: UpdatePrintRequestDto): Promise<PrintRequestDto> {
    const response = await this.client.put<PrintRequestDto>(`/api/requests/${id}`, data);
    return response.data;
  }

  async deleteRequest(id: string): Promise<void> {
    await this.client.delete(`/api/requests/${id}`);
  }

  // Filament endpoints
  async getFilaments(inStock?: boolean): Promise<FilamentDto[]> {
    const params = inStock !== undefined ? { inStock } : {};
    const response = await this.client.get<FilamentDto[]>('/api/filaments', { params });
    return response.data;
  }

  async getFilament(id: string): Promise<FilamentDto> {
    const response = await this.client.get<FilamentDto>(`/api/filaments/${id}`);
    return response.data;
  }

  // Admin endpoints
  async getAdminRequests(): Promise<PrintRequestDto[]> {
    const response = await this.client.get<PrintRequestDto[]>('/api/admin/requests');
    return response.data;
  }

  async changeRequestStatus(id: string, data: ChangeStatusDto): Promise<PrintRequestDto> {
    const response = await this.client.put<PrintRequestDto>(`/api/admin/requests/${id}/status`, data);
    return response.data;
  }

  async updateAdminRequest(id: string, data: UpdatePrintRequestAdminDto): Promise<PrintRequestDto> {
    const response = await this.client.put<PrintRequestDto>(`/api/admin/requests/${id}`, data);
    return response.data;
  }

  async createFilament(data: CreateFilamentDto): Promise<FilamentDto> {
    const response = await this.client.post<FilamentDto>('/api/admin/filaments', data);
    return response.data;
  }

  async updateFilament(id: string, data: UpdateFilamentDto): Promise<FilamentDto> {
    const response = await this.client.put<FilamentDto>(`/api/admin/filaments/${id}`, data);
    return response.data;
  }

  async updateFilamentStock(id: string, data: UpdateStockDto): Promise<FilamentDto> {
    const response = await this.client.patch<FilamentDto>(`/api/admin/filaments/${id}/stock`, data);
    return response.data;
  }

  async deleteFilament(id: string): Promise<void> {
    await this.client.delete(`/api/admin/filaments/${id}`);
  }

  // Filament Request endpoints
  async getFilamentRequests(): Promise<FilamentRequestDto[]> {
    const response = await this.client.get<FilamentRequestDto[]>('/api/filamentrequests');
    return response.data;
  }

  async getFilamentRequest(id: string): Promise<FilamentRequestDto> {
    const response = await this.client.get<FilamentRequestDto>(`/api/filamentrequests/${id}`);
    return response.data;
  }

  async getMyFilamentRequests(): Promise<FilamentRequestDto[]> {
    const response = await this.client.get<FilamentRequestDto[]>('/api/filamentrequests/my-requests');
    return response.data;
  }

  async createFilamentRequest(data: CreateFilamentRequestDto): Promise<FilamentRequestDto> {
    const response = await this.client.post<FilamentRequestDto>('/api/filamentrequests', data);
    return response.data;
  }

  async deleteFilamentRequest(id: string): Promise<void> {
    await this.client.delete(`/api/filamentrequests/${id}`);
  }

  // Admin Filament Request endpoints
  async getAdminFilamentRequests(): Promise<FilamentRequestDto[]> {
    const response = await this.client.get<FilamentRequestDto[]>('/api/admin/filament-requests');
    return response.data;
  }

  async changeFilamentRequestStatus(id: string, data: ChangeFilamentRequestStatusDto): Promise<FilamentRequestDto> {
    const response = await this.client.put<FilamentRequestDto>(`/api/admin/filament-requests/${id}/status`, data);
    return response.data;
  }

  // Token refresh
  async refreshToken(): Promise<string> {
    const response = await this.client.post<{ token: string }>('/api/auth/refresh');
    const newToken = response.data.token;
    this.setToken(newToken);
    return newToken;
  }

  async shouldRefreshToken(): Promise<boolean> {
    const token = this.getToken();
    if (!token) return false;

    try {
      // Try to refresh - backend will validate token expiry securely
      await this.refreshToken();
      return true;
    } catch (error) {
      // If refresh fails, token is invalid or expired
      return false;
    }
  }
}

export const api = new ApiClient();
