// Rules: R-01, B-01 Only feature api calls bridge
import { PipelineBridge } from '../../../bridge/PipelineBridge';
import {
  CreateBiginContactRequest,
  CreateBiginContactResponse,
  ReadBiginContactsRequest,
  ReadBiginContactsResponse,
} from '../../../contracts/dtos';
import { CorrelationId } from '../../../core/ids/correlationId';
import { IdempotencyKey } from '../../../core/ids/idempotencyKey';

export type BiginApi = ReturnType<typeof createBiginApi>;

export function createBiginApi(bridge: PipelineBridge) {
  return {
    async readContacts(request: ReadBiginContactsRequest, correlationId: CorrelationId) {
      return bridge.command<ReadBiginContactsRequest, ReadBiginContactsResponse>(
        'bigin.contacts.read',
        request,
        correlationId
      );
    },

    async createContact(
      request: CreateBiginContactRequest,
      correlationId: CorrelationId,
      idempotencyKey: IdempotencyKey
    ) {
      return bridge.command<CreateBiginContactRequest, CreateBiginContactResponse>(
        'bigin.contacts.create',
        request,
        correlationId,
        idempotencyKey
      );
    },
  };
}

