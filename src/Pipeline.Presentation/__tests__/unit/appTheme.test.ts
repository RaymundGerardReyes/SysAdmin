// Rules: L-01 UI Design System, N-10 Centralized token metadata
import {
  getLayoutBreakpoint,
  resolveColorScheme,
  formatThemeOptionLabel,
  getThemeOptionIndex,
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

  it('[L-01] formats theme option label with resolved system theme in parentheses', () => {
    expect(formatThemeOptionLabel('system', 'Auto', 'dark')).toBe('Auto (Dark)');
    expect(formatThemeOptionLabel('system', 'Auto', 'light')).toBe('Auto (Light)');
    expect(formatThemeOptionLabel('system', 'Auto', null)).toBe('Auto');
    expect(formatThemeOptionLabel('light', 'Light', 'dark')).toBe('Light');
    expect(formatThemeOptionLabel('dark', 'Dark', 'light')).toBe('Dark');
  });

  it('[L-01] indexes theme options accurately for sliding pill layout', () => {
    expect(getThemeOptionIndex('light')).toBe(0);
    expect(getThemeOptionIndex('dark')).toBe(1);
    expect(getThemeOptionIndex('system')).toBe(2);
  });
});
