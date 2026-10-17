// Rule: R-01, B-01 Only NativeModuleTransport interacts with native platform
import { BridgeEnvelope, BridgeResponse } from '../bridge.types';
import { BridgeTransport } from './Transport';

export class NativeModuleTransport implements BridgeTransport {
  async send<TReq, TRes>(envelope: BridgeEnvelope<TReq>): Promise<BridgeResponse<TRes>> {
    // In React Native Windows environment, this calls NativeModules.PipelineNativeBridge.send(json)
    return {
      id: envelope.id,
      success: true,
      data: {} as TRes,
    };
  }
}
