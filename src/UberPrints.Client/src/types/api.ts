// API Types based on backend DTOs

export enum RequestStatusEnum {
  Pending = "Pending",
  Accepted = "Accepted",
  Rejected = "Rejected",
  OnHold = "OnHold",
  Paused = "Paused",
  WaitingForMaterials = "WaitingForMaterials",
  Delivering = "Delivering",
  WaitingForPickup = "WaitingForPickup",
  Completed = "Completed"
}

export enum FilamentRequestStatusEnum {
  Pending = "Pending",
  Approved = "Approved",
  Rejected = "Rejected",
  Ordered = "Ordered",
  Received = "Received"
}

export interface UserDto {
  id: string;
  discordId?: string;
  username: string;
  globalName?: string;
  avatarHash?: string;
  isAdmin: boolean;
  createdAt: string;
}

export interface AdminUserDto {
  id: string;
  discordId?: string;
  guestSessionToken?: string;
  username: string;
  globalName?: string;
  avatarHash?: string;
  isAdmin: boolean;
  createdAt: string;
  printRequestCount: number;
  filamentRequestCount: number;
  isGuest: boolean;
}

export interface ProfileDto {
  id: string;
  discordId?: string;
  username: string;
  globalName?: string;
  avatarHash?: string;
  avatarUrl?: string;
  isAdmin: boolean;
  createdAt: string;
}

export interface UpdateDisplayNameDto {
  displayName: string;
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

export interface PrintRequestChangeDto {
  id: string;
  printRequestId: string;
  fieldName: string;
  oldValue?: string;
  newValue?: string;
  changedByUserId?: string;
  changedByUsername?: string;
  changedAt: string;
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
  notifyOnStatusChange: boolean;
  filamentId?: string;
  filamentName?: string;
  currentStatus: RequestStatusEnum;
  createdAt: string;
  updatedAt: string;
  statusHistory: StatusHistoryDto[];
  changes: PrintRequestChangeDto[];
}

export interface CreatePrintRequestDto {
  requesterName: string;
  modelUrl: string;
  notes?: string;
  requestDelivery: boolean;
  isPublic?: boolean;
  notifyOnStatusChange?: boolean;
  filamentId?: string;
}

export interface UpdatePrintRequestDto {
  requesterName: string;
  modelUrl: string;
  notes?: string;
  requestDelivery: boolean;
  isPublic: boolean;
  filamentId?: string;
}

export interface UpdatePrintRequestAdminDto {
  requesterName: string;
  modelUrl: string;
  notes?: string;
  requestDelivery: boolean;
  isPublic: boolean;
  filamentId?: string;
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
  isAvailable?: boolean;
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
  isAvailable: boolean;
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

// Printer types
export enum PrinterStateEnum {
  Unknown = 'Unknown',
  Idle = 'Idle',
  Busy = 'Busy',
  Printing = 'Printing',
  Paused = 'Paused',
  Finished = 'Finished',
  Stopped = 'Stopped',
  Error = 'Error',
  Attention = 'Attention',
  Ready = 'Ready',
}

export interface PrinterDto {
  id: string;
  name: string;
  ipAddress: string;
  isActive: boolean;
  location?: string;
  currentState: PrinterStateEnum;
  lastStatusUpdate?: string;
  nozzleTemperature?: number;
  nozzleTargetTemperature?: number;
  bedTemperature?: number;
  bedTargetTemperature?: number;
  printProgress?: number;
  timeRemaining?: number;
  timePrinting?: number;
  currentFileName?: string;
  axisX?: number;
  axisY?: number;
  axisZ?: number;
  flowRate?: number;
  speedRate?: number;
  fanHotend?: number;
  fanPrint?: number;
  createdAt: string;
  updatedAt: string;
}

export interface PrinterStatusDto {
  id: string;
  name: string;
  location?: string;
  currentState: PrinterStateEnum;
  lastStatusUpdate?: string;
  nozzleTemperature?: number;
  nozzleTargetTemperature?: number;
  bedTemperature?: number;
  bedTargetTemperature?: number;
  printProgress?: number;
  timeRemaining?: number;
  timePrinting?: number;
  currentFileName?: string;
  axisX?: number;
  axisY?: number;
  axisZ?: number;
  flowRate?: number;
  speedRate?: number;
  fanHotend?: number;
  fanPrint?: number;
  isAvailable: boolean;
}

export interface PrintQueueItem {
  id: string;
  requesterName: string;
  modelUrl: string;
  filamentName?: string;
  acceptedAt: string;
}
