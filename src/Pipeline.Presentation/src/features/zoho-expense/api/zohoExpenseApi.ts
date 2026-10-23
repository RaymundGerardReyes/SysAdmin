// Rules: R-01, B-01 Only feature api calls bridge
import { PipelineBridge } from '../../../bridge/PipelineBridge';
import {
  CreateZohoExpenseRequest,
  CreateZohoExpenseResponse,
  ReadZohoExpensesRequest,
  ReadZohoExpensesResponse,
} from '../../../contracts/dtos';
import { CorrelationId } from '../../../core/ids/correlationId';
import { IdempotencyKey } from '../../../core/ids/idempotencyKey';

export type ZohoExpenseApi = ReturnType<typeof createZohoExpenseApi>;

export function createZohoExpenseApi(bridge: PipelineBridge) {
  return {
    async readExpenses(request: ReadZohoExpensesRequest, correlationId: CorrelationId) {
      return bridge.command<ReadZohoExpensesRequest, ReadZohoExpensesResponse>(
        'zoho.expense.read',
        request,
        correlationId
      );
    },

    async createExpense(
      request: CreateZohoExpenseRequest,
      correlationId: CorrelationId,
      idempotencyKey: IdempotencyKey
    ) {
      return bridge.command<CreateZohoExpenseRequest, CreateZohoExpenseResponse>(
        'zoho.expense.create',
        request,
        correlationId,
        idempotencyKey
      );
    },
  };
}

