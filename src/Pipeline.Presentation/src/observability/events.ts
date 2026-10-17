export const OBSERVABILITY_EVENTS = [
  'intent.started',
  'intent.validated',
  'intent.confirmed',
  'intent.succeeded',
  'intent.failed',
  'intent.challenged',
  'intent.pending_approval',
  'intent.timeout',
  'bridge.command_sent',
  'bridge.response_received',
  'bridge.timeout',
  'capability.refreshed',
  'session.established',
  'session.cleared',
] as const;

export type ObservabilityEventName = typeof OBSERVABILITY_EVENTS[number];
