import { PipelineBridge } from '../../../bridge/PipelineBridge';
import { CorrelationId } from '../../../core/ids/correlationId';

export function createTemplateApi(bridge: PipelineBridge) {
  return {
    async ping(correlationId: CorrelationId) {
      return bridge.command('jobs.read', {}, correlationId);
    },
  };
}
