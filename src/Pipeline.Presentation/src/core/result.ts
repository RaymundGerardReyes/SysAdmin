// Rule: L-03 Pure core
export type Result<T, E> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly error: E };

export function ok<T, E = never>(value: T): Result<T, E> {
  return { ok: true, value };
}

export function err<E, T = never>(error: E): Result<T, E> {
  return { ok: false, error };
}

export function isOk<T, E>(result: Result<T, E>): result is { ok: true; value: T } {
  return result.ok;
}

export function isErr<T, E>(result: Result<T, E>): result is { ok: false; error: E } {
  return !result.ok;
}

export function match<T, E, R>(
  result: Result<T, E>,
  branches: { ok: (value: T) => R; err: (error: E) => R }
): R {
  return result.ok ? branches.ok(result.value) : branches.err(result.error);
}
