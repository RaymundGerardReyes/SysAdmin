// Rule: L-07 The ONLY file in the codebase allowed to call console.*
import { redactSensitiveData } from './redact';

export const secureLogger = {
  info(message: string, context?: Record<string, unknown>): void {
    const sanitizedMsg = redactSensitiveData(message);
    const sanitizedCtx = context ? redactSensitiveData(JSON.stringify(context)) : '';
    console.info(`[INFO] ${sanitizedMsg}`, sanitizedCtx);
  },

  warn(message: string, context?: Record<string, unknown>): void {
    const sanitizedMsg = redactSensitiveData(message);
    const sanitizedCtx = context ? redactSensitiveData(JSON.stringify(context)) : '';
    console.warn(`[WARN] ${sanitizedMsg}`, sanitizedCtx);
  },

  error(message: string, context?: Record<string, unknown>): void {
    const sanitizedMsg = redactSensitiveData(message);
    const sanitizedCtx = context ? redactSensitiveData(JSON.stringify(context)) : '';
    console.error(`[ERROR] ${sanitizedMsg}`, sanitizedCtx);
  },
};
