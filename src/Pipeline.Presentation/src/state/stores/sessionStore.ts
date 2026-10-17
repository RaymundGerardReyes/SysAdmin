import { create } from 'zustand';
import { CapabilitiesContract } from '../../contracts/capabilities';

export interface SessionState {
  isAuthenticated: boolean;
  userId: string | null;
  capabilities: CapabilitiesContract | null;
  setSession: (userId: string, capabilities: CapabilitiesContract) => void;
  clearSession: () => void;
}

export const useSessionStore = create<SessionState>((set) => ({
  isAuthenticated: false,
  userId: null,
  capabilities: null,
  setSession: (userId, capabilities) =>
    set({ isAuthenticated: true, userId, capabilities }),
  clearSession: () =>
    set({ isAuthenticated: false, userId: null, capabilities: null }),
}));
