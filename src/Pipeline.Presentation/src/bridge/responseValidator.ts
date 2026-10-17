// Rule: B-07 Drop token-like fields and sanitize bridge responses
import { BridgeResponse } from './bridge.types';

const FORBIDDEN_FIELDS = new Set([
  'access_token',
  'refresh_token',
  'client_secret',
  'password',
  'token',
  'apiKey',
]);

export function validateAndSanitizeResponse<T>(raw: BridgeResponse<T>): BridgeResponse<T> {
  if (!raw || typeof raw !== 'object') {
    throw new Error('Invalid bridge response: expected an object.');
  }

  if (raw.data && typeof raw.data === 'object') {
    const sanitized = { ...(raw.data as Record<string, unknown>) };
    for (const key of Object.keys(sanitized)) {
      if (FORBIDDEN_FIELDS.has(key)) {
        delete sanitized[key];
      }
    }
    return { ...raw, data: sanitized as T };
  }

  return raw;
}
