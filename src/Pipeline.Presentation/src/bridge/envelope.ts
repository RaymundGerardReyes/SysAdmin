// Rule: B-01 Closed command envelope
import { createRequestId } from '../core/ids/requestId';
import { CorrelationId } from '../core/ids/correlationId';
import { IdempotencyKey } from '../core/ids/idempotencyKey';
import { CommandName } from '../contracts/commands';
import { BridgeEnvelope } from './bridge.types';

export function createBridgeEnvelope<T>(
  command: CommandName,
  payload: T,
  correlationId: CorrelationId,
  idempotencyKey?: IdempotencyKey
): BridgeEnvelope<T> {
  return {
    id: createRequestId(),
    command,
    correlationId,
    idempotencyKey,
    payload,
  };
}
