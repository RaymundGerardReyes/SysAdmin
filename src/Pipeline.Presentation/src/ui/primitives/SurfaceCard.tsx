// Rules: L-01 Pure UI layer, N-06 No inline hex, N-07 Static class maps
import React from 'react';
import { View } from 'react-native';

export type SurfaceVariant = 'elevated' | 'subtle' | 'accent' | 'plain';
export type SurfacePadding = 'none' | 'sm' | 'md' | 'lg';

export interface SurfaceCardProps {
  readonly children: React.ReactNode;
  readonly variant?: SurfaceVariant;
  readonly padding?: SurfacePadding;
  readonly accessibilityLabel?: string;
}

const VARIANT_MAP: Record<SurfaceVariant, string> = {
  elevated: 'bg-canvas-white dark:bg-canvas-900 border border-canvas-200 dark:border-canvas-700 rounded-xl',
  subtle: 'bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700 rounded-xl',
  accent: 'bg-accent-50 dark:bg-accent-950 border border-accent-100 dark:border-accent-900 rounded-xl',
  plain: 'bg-transparent border-b border-canvas-200 dark:border-canvas-800',
};

const PADDING_MAP: Record<SurfacePadding, string> = {
  none: 'p-0',
  sm: 'p-3',
  md: 'p-4',
  lg: 'p-6',
};

export function SurfaceCard({
  children,
  variant = 'elevated',
  padding = 'md',
  accessibilityLabel,
}: SurfaceCardProps) {
  return (
    <View
      accessible={Boolean(accessibilityLabel)}
      accessibilityLabel={accessibilityLabel}
      className={`${VARIANT_MAP[variant]} ${PADDING_MAP[padding]}`}
    >
      {children}
    </View>
  );
}

