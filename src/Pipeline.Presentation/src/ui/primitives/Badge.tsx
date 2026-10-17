// Rules: L-01 Pure UI layer, N-06 No inline hex, N-07 Static class maps
// Accessibility: Never communicate status through color alone; pairs symbol + text
import React from 'react';
import { View, Text } from 'react-native';

export type BadgeTone = 'neutral' | 'accent' | 'success' | 'warning' | 'danger' | 'info';

export interface BadgeProps {
  readonly label: string;
  readonly tone?: BadgeTone;
  readonly showSymbol?: boolean;
}

const CONTAINER_MAP: Record<BadgeTone, string> = {
  neutral: 'bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700',
  accent: 'bg-accent-50 dark:bg-accent-950 border border-accent-100 dark:border-accent-900',
  success: 'bg-status-successBg border border-status-successBorder',
  warning: 'bg-status-warningBg border border-status-warningBorder',
  danger: 'bg-status-dangerBg border border-status-dangerBorder',
  info: 'bg-status-infoBg border border-status-infoBorder',
};

const TEXT_MAP: Record<BadgeTone, string> = {
  neutral: 'text-canvas-900 dark:text-canvas-50',
  accent: 'text-accent-600 dark:text-accent-400',
  success: 'text-status-successText',
  warning: 'text-status-warningText',
  danger: 'text-status-dangerText',
  info: 'text-status-infoText',
};

const SYMBOL_MAP: Record<BadgeTone, string> = {
  neutral: '•',
  accent: '◆',
  success: '✓',
  warning: '!',
  danger: '×',
  info: 'i',
};

export function Badge({ label, tone = 'neutral', showSymbol = true }: BadgeProps) {
  return (
    <View
      accessibilityRole="text"
      accessibilityLabel={`${tone} status: ${label}`}
      className={`${CONTAINER_MAP[tone]} px-2.5 py-1 rounded-sm flex-row items-center self-start`}
    >
      {showSymbol && (
        <Text className={`${TEXT_MAP[tone]} text-xs font-bold mr-1`}>
          {SYMBOL_MAP[tone]}
        </Text>
      )}
      <Text className={`${TEXT_MAP[tone]} text-xs font-medium`}>{label}</Text>
    </View>
  );
}

