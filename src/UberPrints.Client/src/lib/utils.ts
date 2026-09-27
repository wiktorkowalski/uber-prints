import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"
import { RequestStatusEnum, UserDto } from "../types/api"
import { STATUS_LABELS } from "./requestStage"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function getStatusLabel(status: RequestStatusEnum): string {
  return STATUS_LABELS[status] || 'Unknown';
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

/**
 * Gets the display name for a user.
 * Prefers globalName (Discord display name) over username.
 * @param user - The user object
 * @returns The display name to show in the UI
 */
export function getDisplayName(user: UserDto | null | undefined): string {
  if (!user) return 'Unknown';
  return user.globalName || user.username || 'Unknown';
}

/** Short date like "25 Sep", with the year added when it is not the current year. */
export function formatShortDate(dateString: string): string {
  const date = new Date(dateString);
  const sameYear = date.getFullYear() === new Date().getFullYear();
  return new Intl.DateTimeFormat('en-GB', {
    day: 'numeric',
    month: 'short',
    ...(sameYear ? {} : { year: 'numeric' }),
  }).format(date);
}

/** Relative time for the last week, short date after that. Fits narrow table cells. */
export function formatCompactTime(dateString: string): string {
  const diffDays = (Date.now() - new Date(dateString).getTime()) / 86_400_000;
  return diffDays < 7 ? formatRelativeTime(dateString) : formatShortDate(dateString);
}

/** Hostname of a model URL without "www.", or the raw string when it does not parse. */
export function getModelHost(url: string): string {
  try {
    return new URL(url).hostname.replace(/^www\./, '');
  } catch {
    return url;
  }
}

/** Short date with time like "25 Sep 18:11". */
export function formatShortDateTime(dateString: string): string {
  const time = new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit' }).format(new Date(dateString));
  return `${formatShortDate(dateString)} ${time}`;
}
