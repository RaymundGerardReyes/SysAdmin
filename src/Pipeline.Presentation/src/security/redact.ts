// Rule: L-07 Structured logging & automatic redaction
export function redactSensitiveData(input: string): string {
  if (!input) return '';
  return input
    .replace(/Bearer\s+[A-Za-z0-9\-_\.]+/gi, 'Bearer [REDACTED]')
    .replace(/"(access_token|refresh_token|client_secret|password)"\s*:\s*"[^"]*"/gi, '"$1": "[REDACTED]"')
    .replace(/(Authorization|Cookie|X-Api-Key)\s*[:=]\s*[^\r\n,;]+/gi, '$1: [REDACTED]');
}

export function redactObject<T extends Record<string, unknown>>(obj: T): T {
  const json = JSON.stringify(obj);
  return JSON.parse(redactSensitiveData(json)) as T;
}
