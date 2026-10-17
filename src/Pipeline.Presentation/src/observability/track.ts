// Rule: L-06 Every user action has an identity, L-07 Structured logging & automatic redaction
import { ObservabilityEventName } from './events';
import { breadcrumbs } from './breadcrumbs';
import { redactObject } from '../security/redact';
import { secureLogger } from '../security/secureLogger';

export function track(
  event: ObservabilityEventName,
  fields?: Record<string, unknown>
): void {
  const sanitizedFields = fields ? redactObject(fields) : {};
  const timestamp = new Date().toISOString();

  breadcrumbs.add({
    timestamp,
    event,
    payload: sanitizedFields,
  });

  secureLogger.info(`[EVENT] ${event}`, sanitizedFields);
}
