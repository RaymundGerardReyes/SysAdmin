// Rule: L-06 Every user action has an identity
export type CorrelationId = string & { readonly __brand: unique symbol };

export function createCorrelationId(): CorrelationId {
  return `c_${Math.random().toString(36).slice(2, 11)}_${Date.now()}` as CorrelationId;
}
