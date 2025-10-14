import axios, { AxiosInstance } from 'axios';
import {
  PrintRequestDto,
  CreatePrintRequestDto,
  UpdatePrintRequestDto,
  FilamentDto,
  UserDto,
  ChangeStatusDto,
  CreateFilamentDto,
  UpdateFilamentDto,
  UpdateStockDto,
  GuestSessionResponse,
} from '../types/api';

class ApiClient {
  private client: AxiosInstance;
  private baseURL: string;

  constructor() {
    this.baseURL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7001';

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

    // Handle 401 responses
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          this.clearToken();
          this.clearGuestSessionToken();
          // Dispatch custom event for AuthContext to handle logout gracefully
          window.dispatchEvent(new CustomEvent('auth:unauthorized'));
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
}

export const api = new ApiClient();
