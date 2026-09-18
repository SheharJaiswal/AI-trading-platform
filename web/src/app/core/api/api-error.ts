export interface ApiErrorPayload {
  errorCode?: string;
  message?: string;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!error || typeof error !== 'object') return fallback;

  const response = error as { error?: unknown };
  const payload = response.error;
  if (payload && typeof payload === 'object') {
    const typed = payload as ApiErrorPayload;
    if (typeof typed.message === 'string' && typed.message.trim()) return typed.message;
    if (typeof typed.errorCode === 'string' && typed.errorCode.trim()) return typed.errorCode;
  }

  return fallback;
}
