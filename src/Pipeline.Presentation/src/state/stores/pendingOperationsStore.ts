import { create } from 'zustand';
import { IdempotencyKey } from '../../core/ids/idempotencyKey';
import { CorrelationId } from '../../core/ids/correlationId';

export interface PendingOperation {
  idempotencyKey: IdempotencyKey;
  correlationId: CorrelationId;
  command: string;
  startedAt: number;
}

export interface PendingOperationsState {
  operations: Map<string, PendingOperation>;
  add: (op: PendingOperation) => void;
  remove: (key: IdempotencyKey) => void;
  clear: () => void;
}

export const usePendingOperationsStore = create<PendingOperationsState>((set) => ({
  operations: new Map(),
  add: (op) =>
    set((state) => {
      const next = new Map(state.operations);
      next.set(op.idempotencyKey, op);
      return { operations: next };
    }),
  remove: (key) =>
    set((state) => {
      const next = new Map(state.operations);
      next.delete(key);
      return { operations: next };
    }),
  clear: () => set({ operations: new Map() }),
}));
