import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"
import { RequestStatusEnum } from "../types/api"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function getStatusLabel(status: RequestStatusEnum): string {
  const labels: Record<RequestStatusEnum, string> = {
    [RequestStatusEnum.Pending]: 'Pending',
    [RequestStatusEnum.Accepted]: 'Accepted',
    [RequestStatusEnum.Rejected]: 'Rejected',
    [RequestStatusEnum.OnHold]: 'On Hold',
    [RequestStatusEnum.Paused]: 'Paused',
    [RequestStatusEnum.WaitingForMaterials]: 'Waiting for Materials',
    [RequestStatusEnum.Delivering]: 'Delivering',
    [RequestStatusEnum.WaitingForPickup]: 'Waiting for Pickup',
    [RequestStatusEnum.Completed]: 'Completed',
  };
  return labels[status] || 'Unknown';
}

export function getStatusColor(status: RequestStatusEnum): string {
  const colors: Record<RequestStatusEnum, string> = {
    [RequestStatusEnum.Pending]: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300',
    [RequestStatusEnum.Accepted]: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
    [RequestStatusEnum.Rejected]: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300',
    [RequestStatusEnum.OnHold]: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-300',
    [RequestStatusEnum.Paused]: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-300',
    [RequestStatusEnum.WaitingForMaterials]: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-300',
    [RequestStatusEnum.Delivering]: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-300',
    [RequestStatusEnum.WaitingForPickup]: 'bg-cyan-100 text-cyan-800 dark:bg-cyan-900 dark:text-cyan-300',
    [RequestStatusEnum.Completed]: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
  };
  return colors[status] || 'bg-gray-100 text-gray-800';
}

export function formatDate(dateString: string): string {
  const date = new Date(dateString);
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

export function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMins / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffMins < 1) return 'just now';
  if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
  if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;

  return formatDate(dateString);
}

export function sanitizeUrl(url: string): string {
  if (!url) return '#';

  try {
    const parsed = new URL(url);
    // Only allow http and https protocols
    if (parsed.protocol === 'http:' || parsed.protocol === 'https:') {
      return url;
    }
    return '#';
  } catch {
    // Invalid URL
    return '#';
  }
}

export function isSafeUrl(url: string): boolean {
  return sanitizeUrl(url) !== '#';
}
