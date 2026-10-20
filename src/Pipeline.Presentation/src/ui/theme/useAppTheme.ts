// Rules: L-01 UI Design System, N-10 Centralized token metadata
import { useWindowDimensions, useColorScheme as useRNColorScheme } from 'react-native';
import { useColorScheme as useNativeWindColorScheme } from 'nativewind';
import { tokens } from './tokens';
import { getLayoutBreakpoint, resolveColorScheme } from './themeLogic';

export type { LayoutBreakpoint, ThemePreference, ColorScheme } from './themeLogic';
export { getLayoutBreakpoint, resolveColorScheme } from './themeLogic';

export function useAppTheme() {
  const { width, height } = useWindowDimensions();
  const rnScheme = useRNColorScheme();
  const nw = useNativeWindColorScheme();

  const activeColorScheme = resolveColorScheme(nw?.colorScheme, rnScheme);
  const isDark = activeColorScheme === 'dark';
  const breakpoint = getLayoutBreakpoint(width);

  const gridCols = breakpoint === 'expanded' ? 3 : breakpoint === 'medium' ? 2 : 1;
  const contentPadding = breakpoint === 'compact' ? 16 : 28;

  return {
    tokens,
    width,
    height,
    breakpoint,
    isCompact: breakpoint === 'compact',
    isMedium: breakpoint === 'medium',
    isExpanded: breakpoint === 'expanded',
    gridCols,
    contentPadding,
    colorScheme: activeColorScheme,
    isDark,
    setColorScheme: nw?.setColorScheme,
  };
}
