// Rules: L-01 UI Design System, N-10 Centralized token metadata
import { useWindowDimensions } from 'react-native';
import { tokens } from './tokens';

export type LayoutBreakpoint = 'compact' | 'medium' | 'expanded';

export function useAppTheme() {
  const { width } = useWindowDimensions();

  let breakpoint: LayoutBreakpoint = 'compact';
  if (width >= tokens.breakpoints.expandedMin) {
    breakpoint = 'expanded';
  } else if (width >= tokens.breakpoints.mediumMin) {
    breakpoint = 'medium';
  }

  return {
    tokens,
    width,
    breakpoint,
    isCompact: breakpoint === 'compact',
    isMedium: breakpoint === 'medium',
    isExpanded: breakpoint === 'expanded',
  };
}
