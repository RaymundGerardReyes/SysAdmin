// Rules: R-01 One bridge door, B-01 Closed command envelope, B-02 Timeout policy, B-07 Drop token fields
import { CommandName } from '../contracts/commands';
import { CorrelationId } from '../core/ids/correlationId';
import { IdempotencyKey } from '../core/ids/idempotencyKey';
import { Result, ok, err } from '../core/result';
import { AppError, createAppError } from '../contracts/errors/appError';
import { BridgeTransport } from './transports/Transport';
import { createBridgeEnvelope } from './envelope';
import { validateAndSanitizeResponse } from './responseValidator';
import { mapBridgeResponseToAppError } from './errorMapper';
import { getCommandDescriptor } from './commandRegistry';
import { getTimeoutMsForCommand } from './timeouts';

export class PipelineBridge {
  constructor(private readonly transport: BridgeTransport) {}

  async command<TReq, TRes>(
    commandName: CommandName,
    payload: TReq,
    correlationId: CorrelationId,
    idempotencyKey?: IdempotencyKey
  ): Promise<Result<TRes, AppError>> {
    const descriptor = getCommandDescriptor(commandName);
    const envelope = createBridgeEnvelope(commandName, payload, correlationId, idempotencyKey);
    const timeoutMs = getTimeoutMsForCommand(descriptor.mutating);

    let timerId: ReturnType<typeof setTimeout> | undefined;
    try {
      const timeoutPromise = new Promise<never>((_, reject) => {
        timerId = setTimeout(() => reject(new Error('BRIDGE_TIMEOUT')), timeoutMs);
      });

      const responsePromise = this.transport.send<TReq, TRes>(envelope);
      const rawResponse = await Promise.race([responsePromise, timeoutPromise]);

      const sanitized = validateAndSanitizeResponse(rawResponse);

      if (!sanitized.success) {
        const appError = mapBridgeResponseToAppError(sanitized, correlationId);
        return err(appError);
      }

      return ok(sanitized.data as TRes);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Unknown bridge error';
      if (msg === 'BRIDGE_TIMEOUT') {
        return err(createAppError('TIMEOUT', `Command '${commandName}' timed out after ${timeoutMs}ms`, correlationId));
      }
      return err(createAppError('BRIDGE_CONTRACT_MISMATCH', msg, correlationId));
    } finally {
      if (timerId !== undefined) {
        clearTimeout(timerId);
      }
    }
  }
}
