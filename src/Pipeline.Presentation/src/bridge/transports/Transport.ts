import { BridgeEnvelope, BridgeResponse } from '../bridge.types';

export interface BridgeTransport {
  send<TReq, TRes>(envelope: BridgeEnvelope<TReq>): Promise<BridgeResponse<TRes>>;
}
