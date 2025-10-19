// API Types based on backend DTOs

export enum RequestStatusEnum {
  Pending = 0,
  Accepted = 1,
  Rejected = 2,
  OnHold = 3,
  Paused = 4,
  WaitingForMaterials = 5,
  Delivering = 6,
  WaitingForPickup = 7,
  Completed = 8
}

export enum FilamentRequestStatusEnum {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Ordered = 3,
  Received = 4
}

export interface UserDto {
  id: string;
  discordId?: string;
  username: string;
  email?: string;
  isAdmin: boolean;
  createdAt: string;
}

export interface FilamentDto {
  id: string;
  name: string;
  material: string;
  brand: string;
  colour: string;
  stockAmount: number;
  stockUnit: string;
  link?: string;
  photoUrl?: string;
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface StatusHistoryDto {
  id: string;
  requestId: string;
  status: RequestStatusEnum;
  changedByUserId?: string;
  changedByUsername?: string;
  adminNotes?: string;
  timestamp: string;
}

export interface PrintRequestDto {
  id: string;
  userId?: string;
  guestTrackingToken?: string;
  requesterName: string;
  modelUrl: string;
  notes?: string;
  requestDelivery: boolean;
  isPublic: boolean;
  filamentId: string;
  filamentName: string;
  currentStatus: RequestStatusEnum;
  createdAt: string;
  updatedAt: string;
  statusHistory: StatusHistoryDto[];
}

export interface CreatePrintRequestDto {
  requesterName: string;
  modelUrl: string;
  notes?: string;
  requestDelivery: boolean;
  isPublic?: boolean;
  filamentId: string;
}

export interface UpdatePrintRequestDto {
  requesterName: string;
  modelUrl: string;
  notes?: string;
  requestDelivery: boolean;
  isPublic: boolean;
  filamentId: string;
}

export interface ChangeStatusDto {
  status: RequestStatusEnum;
  adminNotes?: string;
}

export interface CreateFilamentDto {
  name: string;
  material: string;
  brand: string;
  colour: string;
  stockAmount: number;
  stockUnit: string;
  link?: string;
  photoUrl?: string;
}

export interface UpdateFilamentDto {
  name: string;
  material: string;
  brand: string;
  colour: string;
  stockAmount: number;
  stockUnit: string;
  link?: string;
  photoUrl?: string;
}

export interface UpdateStockDto {
  stockAmount: number;
}

export interface AuthResponseDto {
  token: string;
  user: UserDto;
}

export interface GuestSessionResponse {
  guestSessionToken: string;
  userId: string;
}

export interface FilamentRequestStatusHistoryDto {
  id: string;
  status: FilamentRequestStatusEnum;
  reason?: string;
  changedByUserId?: string;
  changedByUsername?: string;
  createdAt: string;
}

export interface FilamentRequestDto {
  id: string;
  userId?: string;
  requesterName: string;
  material: string;
  brand: string;
  colour: string;
  link?: string;
  notes?: string;
  currentStatus: FilamentRequestStatusEnum;
  filamentId?: string;
  filamentName?: string;
  createdAt: string;
  updatedAt: string;
  statusHistory: FilamentRequestStatusHistoryDto[];
}

export interface CreateFilamentRequestDto {
  requesterName: string;
  material: string;
  brand: string;
  colour: string;
  link?: string;
  notes?: string;
}

export interface ChangeFilamentRequestStatusDto {
  status: FilamentRequestStatusEnum;
  reason?: string;
  filamentId?: string;
}
