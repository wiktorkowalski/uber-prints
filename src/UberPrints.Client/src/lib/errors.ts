import { isAxiosError } from 'axios';

/** HTTP status of a failed API call, or undefined when the error has no response. */
export function getHttpStatus(error: unknown): number | undefined {
  return isAxiosError(error) ? error.response?.status : undefined;
}

/** Backend `message` from a failed API call, or the fallback when there is none. */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message || fallback;
  }
  return fallback;
}
