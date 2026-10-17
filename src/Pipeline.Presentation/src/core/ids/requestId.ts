export type RequestId = string & { readonly __brand: unique symbol };

export function createRequestId(): RequestId {
  return `req_${Math.random().toString(36).slice(2, 11)}` as RequestId;
}
