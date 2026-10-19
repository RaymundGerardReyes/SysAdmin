// Rules: L-01 UI Design System, N-10 Centralized token metadata
import { useWindowDimensions, useColorScheme as useRNColorScheme } from 'react-native';
import { useColorScheme as useNativeWindColorScheme } from 'nativewind';
import { tokens } from './tokens';

export type LayoutBreakpoint = 'compact' | 'medium' | 'expanded';
export type ThemePreference = 'light' | 'dark' | 'system';

export function useAppTheme() {
  const { width, height } = useWindowDimensions();
  const rnColorScheme = useRNColorScheme();
  const nativeWindColorScheme = useNativeWindColorScheme();

  let breakpoint: LayoutBreakpoint = 'compact';
  if (width >= tokens.breakpoints.expandedMin) {
    breakpoint = 'expanded';
  } else if (width >= tokens.breakpoints.mediumMin) {
    breakpoint = 'medium';
  }

  return {
    tokens,
    width,
    height,
    breakpoint,
    isCompact: breakpoint === 'compact',
    isMedium: breakpoint === 'medium',
    isExpanded: breakpoint === 'expanded',
    colorScheme: nativeWindColorScheme.colorScheme ?? rnColorScheme ?? 'light',
    isDark: (nativeWindColorScheme.colorScheme ?? rnColorScheme ?? 'light') === 'dark',
    setColorScheme: nativeWindColorScheme.setColorScheme,
  };
}
