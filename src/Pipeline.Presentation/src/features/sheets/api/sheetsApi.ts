// Rule: R-01, B-01 Only feature api calls bridge
import { PipelineBridge } from '../../../bridge/PipelineBridge';
import { CorrelationId } from '../../../core/ids/correlationId';
import { IdempotencyKey } from '../../../core/ids/idempotencyKey';

export type SheetsApi = ReturnType<typeof createSheetsApi>;

export function createSheetsApi(bridge: PipelineBridge) {
  return {
    async readRange(spreadsheetId: string, range: string, correlationId: CorrelationId) {
      return bridge.command<{ spreadsheetId: string; range: string }, { range: string; values: string[][] }>(
        'sheets.read',
        { spreadsheetId, range },
        correlationId
      );
    },

    async writeRange(
      spreadsheetId: string,
      range: string,
      values: string[][],
      correlationId: CorrelationId,
      idempotencyKey: IdempotencyKey
    ) {
      return bridge.command<{ spreadsheetId: string; range: string; values: string[][] }, { updatedRows: number }>(
        'sheets.write',
        { spreadsheetId, range, values },
        correlationId,
        idempotencyKey
      );
    },
  };
}
