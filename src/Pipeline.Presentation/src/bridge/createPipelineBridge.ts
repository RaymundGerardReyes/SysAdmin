import { PipelineBridge } from './PipelineBridge';
import { BridgeTransport } from './transports/Transport';
import { MockTransport } from './transports/MockTransport';

export function createPipelineBridge(transport?: BridgeTransport): PipelineBridge {
  return new PipelineBridge(transport ?? new MockTransport());
}
