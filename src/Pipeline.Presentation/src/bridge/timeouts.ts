// Rule: B-02 Timeout policy (never blind retry on write timeout)
export const DEFAULT_READ_TIMEOUT_MS = 10_000;
export const DEFAULT_WRITE_TIMEOUT_MS = 15_000;

export function getTimeoutMsForCommand(mutating: boolean): number {
  return mutating ? DEFAULT_WRITE_TIMEOUT_MS : DEFAULT_READ_TIMEOUT_MS;
}
