// Rule: R-01, B-01 Only feature api calls bridge
import { PipelineBridge } from '../../../bridge/PipelineBridge';
import { CorrelationId } from '../../../core/ids/correlationId';
import { IdempotencyKey } from '../../../core/ids/idempotencyKey';

export type GmailApi = ReturnType<typeof createGmailApi>;

export function createGmailApi(bridge: PipelineBridge) {
  return {
    async sendMessage(
      recipientEmail: string,
      subject: string,
      body: string,
      correlationId: CorrelationId,
      idempotencyKey: IdempotencyKey
    ) {
      return bridge.command<
        { recipientEmail: string; subject: string; body: string },
        { messageId: string; threadId: string }
      >('gmail.send', { recipientEmail, subject, body }, correlationId, idempotencyKey);
    },
  };
}
