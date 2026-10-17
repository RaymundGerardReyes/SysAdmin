// Rules: R-01 One bridge door, B-01 Closed envelope, B-07 Drop token fields, B-02 Timeout
import { PipelineBridge } from '../../src/bridge/PipelineBridge';
import { MockTransport } from '../../src/bridge/transports/MockTransport';
import { createCorrelationId } from '../../src/core/ids/correlationId';
import { createIdempotencyKey } from '../../src/core/ids/idempotencyKey';
import { isOk, isErr } from '../../src/core/result';

describe('PipelineBridge', () => {
  it('[R-01, B-01] sends closed envelope and receives success', async () => {
    const transport = new MockTransport();
    transport.registerHandler('sheets.read', async (env) => {
      return {
        id: env.id,
        success: true,
        data: { range: 'A1:B2', values: [['1', '2']] },
      };
    });

    const bridge = new PipelineBridge(transport);
    const correlationId = createCorrelationId();
    const result = await bridge.command('sheets.read', { spreadsheetId: 'sheet_1', range: 'A1:B2' }, correlationId);

    expect(isOk(result)).toBe(true);
    if (isOk(result)) {
      expect((result.value as any).range).toBe('A1:B2');
    }
  });

  it('[B-07] drops token-like fields from bridge responses', async () => {
    const transport = new MockTransport();
    transport.registerHandler('sheets.read', async (env) => {
      return {
        id: env.id,
        success: true,
        data: {
          range: 'A1',
          access_token: 'secret_leak_attempt',
          refresh_token: 'refresh_leak',
          client_secret: 'cs_leak',
        },
      };
    });

    const bridge = new PipelineBridge(transport);
    const correlationId = createCorrelationId();
    const result = await bridge.command('sheets.read', {}, correlationId);

    expect(isOk(result)).toBe(true);
    if (isOk(result)) {
      const data = result.value as any;
      expect(data.range).toBe('A1');
      expect(data.access_token).toBeUndefined();
      expect(data.refresh_token).toBeUndefined();
      expect(data.client_secret).toBeUndefined();
    }
  });

  it('[B-02] maps transport failure to AppError', async () => {
    const transport = new MockTransport();
    transport.registerHandler('sheets.read', async (env) => {
      return {
        id: env.id,
        success: false,
        errorCode: 'PERMISSION_MISSING',
        errorMessage: 'User lacks sheets.values.read',
        pathId: 'AUTHZ_P05',
      };
    });

    const bridge = new PipelineBridge(transport);
    const correlationId = createCorrelationId();
    const result = await bridge.command('sheets.read', {}, correlationId);

    expect(isErr(result)).toBe(true);
    if (isErr(result)) {
      expect(result.error.code).toBe('PERMISSION_MISSING');
      expect(result.error.pathId).toBe('AUTHZ_P05');
      expect(result.error.correlationId).toBe(correlationId);
    }
  });
});
