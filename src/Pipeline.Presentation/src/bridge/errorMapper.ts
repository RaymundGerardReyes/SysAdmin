import { BridgeResponse } from './bridge.types';
import { AppError, createAppError } from '../contracts/errors/appError';
import { CorrelationId } from '../core/ids/correlationId';
import { ErrorCode } from '../contracts/errors/errorCodes';

export function mapBridgeResponseToAppError(
  response: BridgeResponse,
  correlationId: CorrelationId
): AppError {
  const code = (response.errorCode ?? 'INTERNAL_DENY') as ErrorCode;
  const message = response.errorMessage ?? 'Bridge operation failed.';
  return createAppError(code, message, correlationId, response.pathId, response.approvalId);
}
