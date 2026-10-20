import { create } from 'zustand';
import type { ThemePreference } from '../../ui/theme/themeLogic';

export interface UiState {
  themeMode: ThemePreference;
  sidebarOpen: boolean;
  setThemeMode: (mode: ThemePreference) => void;
  toggleSidebar: () => void;
}

export const useUiStore = create<UiState>((set) => ({
  themeMode: 'system',
  sidebarOpen: true,
  setThemeMode: (themeMode) => set({ themeMode }),
  toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
}));
