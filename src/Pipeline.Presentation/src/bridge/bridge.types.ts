import { RequestId } from '../core/ids/requestId';
import { CorrelationId } from '../core/ids/correlationId';
import { IdempotencyKey } from '../core/ids/idempotencyKey';
import { CommandName } from '../contracts/commands';

export interface BridgeEnvelope<TPayload = unknown> {
  readonly id: RequestId;
  readonly command: CommandName;
  readonly correlationId: CorrelationId;
  readonly idempotencyKey?: IdempotencyKey;
  readonly payload: TPayload;
}

export interface BridgeResponse<TData = unknown> {
  readonly id: RequestId;
  readonly success: boolean;
  readonly data?: TData;
  readonly errorCode?: string;
  readonly errorMessage?: string;
  readonly pathId?: string;
  readonly approvalId?: string;
}
