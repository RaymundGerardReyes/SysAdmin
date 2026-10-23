// Rules: R-01, B-01 Only feature api calls bridge
import { PipelineBridge } from '../../../bridge/PipelineBridge';
import { CreateChatMessageRequest, CreateChatMessageResponse } from '../../../contracts/dtos';
import { CorrelationId } from '../../../core/ids/correlationId';
import { IdempotencyKey } from '../../../core/ids/idempotencyKey';

export type ChatApi = ReturnType<typeof createChatApi>;

export function createChatApi(bridge: PipelineBridge) {
  return {
    async postMessage(
      request: CreateChatMessageRequest,
      correlationId: CorrelationId,
      idempotencyKey: IdempotencyKey
    ) {
      return bridge.command<CreateChatMessageRequest, CreateChatMessageResponse>(
        'chat.create',
        request,
        correlationId,
        idempotencyKey
      );
    },
  };
}

