import { ErrorCode } from './errorCodes';

export interface AppError {
  readonly code: ErrorCode | 'BRIDGE_CONTRACT_MISMATCH' | 'NETWORK_ERROR' | 'TIMEOUT';
  readonly message: string;
  readonly correlationId: string;
  readonly pathId?: string;
  readonly approvalId?: string;
  readonly details?: Record<string, unknown>;
}

export function createAppError(
  code: AppError['code'],
  message: string,
  correlationId: string,
  pathId?: string,
  approvalId?: string
): AppError {
  return {
    code,
    message,
    correlationId,
    ...(pathId !== undefined ? { pathId } : {}),
    ...(approvalId !== undefined ? { approvalId } : {}),
  };
}
