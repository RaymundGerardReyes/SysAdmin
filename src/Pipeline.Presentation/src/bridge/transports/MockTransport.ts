import { BridgeEnvelope, BridgeResponse } from '../bridge.types';
import { BridgeTransport } from './Transport';

export class MockTransport implements BridgeTransport {
  private handlers = new Map<string, (env: BridgeEnvelope<unknown>) => Promise<BridgeResponse<unknown>>>();

  registerHandler<TReq, TRes>(
    command: string,
    handler: (envelope: BridgeEnvelope<TReq>) => Promise<BridgeResponse<TRes>>
  ): void {
    this.handlers.set(command, handler as (env: BridgeEnvelope<unknown>) => Promise<BridgeResponse<unknown>>);
  }

  async send<TReq, TRes>(envelope: BridgeEnvelope<TReq>): Promise<BridgeResponse<TRes>> {
    const handler = this.handlers.get(envelope.command);
    if (handler) {
      const res = await handler(envelope as BridgeEnvelope<unknown>);
      return res as BridgeResponse<TRes>;
    }

    // Default mock response: success
    return {
      id: envelope.id,
      success: true,
      data: { mockSuccess: true } as TRes,
    };
  }
}
