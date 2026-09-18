import {ApiError} from './trading-api.models';

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!error || typeof error !== 'object') return fallback;

  const payload = (error as { error?: unknown }).error;
  if (payload && typeof payload === 'object') {
    const typed = payload as ApiError;
    if (typeof typed.message === 'string' && typed.message.trim()) return typed.message;
    if (typeof typed.errorCode === 'string' && typed.errorCode.trim()) return typed.errorCode;
  }

  return fallback;
}
