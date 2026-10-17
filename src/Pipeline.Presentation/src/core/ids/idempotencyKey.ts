// Rule: L-06 Every user action has an identity
export type IdempotencyKey = string & { readonly __brand: unique symbol };

export function createIdempotencyKey(): IdempotencyKey {
  return `idem_${Math.random().toString(36).slice(2, 11)}_${Date.now()}` as IdempotencyKey;
}
