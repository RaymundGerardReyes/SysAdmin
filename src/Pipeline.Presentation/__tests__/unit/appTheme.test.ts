// Rules: L-01 UI Design System, N-10 Centralized token metadata
import {
  getLayoutBreakpoint,
  resolveColorScheme,
} from '../../src/ui/theme/themeLogic';

describe('app theme helpers', () => {
  it('[L-01] maps breakpoint boundaries deterministically', () => {
    expect(getLayoutBreakpoint(Number.NaN)).toBe('compact');
    expect(getLayoutBreakpoint(767)).toBe('compact');
    expect(getLayoutBreakpoint(768)).toBe('medium');
    expect(getLayoutBreakpoint(1099)).toBe('medium');
    expect(getLayoutBreakpoint(1100)).toBe('expanded');
  });

  it('[L-01] falls back to the system scheme before light', () => {
    expect(resolveColorScheme('dark', 'light')).toBe('dark');
    expect(resolveColorScheme(undefined, 'dark')).toBe('dark');
    expect(resolveColorScheme('system', null)).toBe('light');
  });
});
