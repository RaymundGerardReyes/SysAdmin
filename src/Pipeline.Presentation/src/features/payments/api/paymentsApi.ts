// Rule: R-01, B-01 Only feature api calls bridge
import { PipelineBridge } from '../../../bridge/PipelineBridge';
import { CorrelationId } from '../../../core/ids/correlationId';
import { IdempotencyKey } from '../../../core/ids/idempotencyKey';

export function createPaymentsApi(bridge: PipelineBridge) {
  return {
    async createCheckout(
      accountId: string,
      amount: number,
      currency: string,
      correlationId: CorrelationId,
      idempotencyKey: IdempotencyKey
    ) {
      return bridge.command<
        { accountId: string; amount: number; currency: string },
        { checkoutId: string; status: string }
      >('payments.checkout.create', { accountId, amount, currency }, correlationId, idempotencyKey);
    },

    async approveCheckout(
      approvalId: string,
      checkoutId: string,
      correlationId: CorrelationId
    ) {
      return bridge.command<{ approvalId: string; checkoutId: string }, { approved: boolean }>(
        'payments.checkout.approve',
        { approvalId, checkoutId },
        correlationId
      );
    },
  };
}
