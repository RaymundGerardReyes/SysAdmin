// Rules: L-01 UI Design System, N-10 Centralized token metadata
import { tokens } from './tokens';

export type LayoutBreakpoint = 'compact' | 'medium' | 'expanded';
export type ThemePreference = 'light' | 'dark' | 'system';
export type ColorScheme = Exclude<ThemePreference, 'system'>;

export function getLayoutBreakpoint(width: number): LayoutBreakpoint {
  if (!Number.isFinite(width) || width < tokens.breakpoints.mediumMin) {
    return 'compact';
  }

  if (width >= tokens.breakpoints.expandedMin) {
    return 'expanded';
  }

  return 'medium';
}

export function resolveColorScheme(
  nativeWindScheme: string | null | undefined,
  reactNativeScheme: string | null | undefined
): ColorScheme {
  if (nativeWindScheme === 'light' || nativeWindScheme === 'dark') {
    return nativeWindScheme;
  }

  if (reactNativeScheme === 'light' || reactNativeScheme === 'dark') {
    return reactNativeScheme;
  }

  return 'light';
}
