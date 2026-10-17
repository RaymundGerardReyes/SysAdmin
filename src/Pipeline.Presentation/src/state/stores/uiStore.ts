import { create } from 'zustand';

export interface UiState {
  themeMode: 'light' | 'dark' | 'system';
  sidebarOpen: boolean;
  setThemeMode: (mode: 'light' | 'dark' | 'system') => void;
  toggleSidebar: () => void;
}

export const useUiStore = create<UiState>((set) => ({
  themeMode: 'system',
  sidebarOpen: true,
  setThemeMode: (themeMode) => set({ themeMode }),
  toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
}));
