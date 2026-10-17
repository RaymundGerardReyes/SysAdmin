import { useSessionStore } from './sessionStore';
import { usePendingOperationsStore } from './pendingOperationsStore';
import { appQueryClient } from '../queryClient';

export function resetAllStores(): void {
  useSessionStore.getState().clearSession();
  usePendingOperationsStore.getState().clear();
  appQueryClient.clear();
}
