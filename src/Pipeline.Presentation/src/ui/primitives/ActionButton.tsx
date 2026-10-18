// Rules: L-01 Pure UI layer, R-07 Double-submit guard while Submitting, N-06, N-07
// Accessibility: Enforces min-h-[44px] touch target and descriptive busy state
import React from 'react';
import { Pressable, Text, ActivityIndicator, View } from 'react-native';

export type ButtonVariant = 'primary' | 'secondary' | 'tertiary' | 'destructive';

export interface ActionButtonProps {
  readonly label: string;
  readonly onPress: () => void;
  readonly variant?: ButtonVariant | undefined;
  readonly isSubmitting?: boolean | undefined;
  readonly submittingLabel?: string | undefined;
  readonly disabled?: boolean | undefined;
  readonly disabledReason?: string | undefined;
  readonly fullWidth?: boolean | undefined;
  readonly accessibilityHint?: string | undefined;
}

const SURFACE_MAP: Record<ButtonVariant, string> = {
  primary: 'bg-accent-600 active:bg-accent-700 hover:bg-accent-700 border border-accent-600 focus:border-accent-400',
  secondary: 'bg-canvas-white dark:bg-canvas-900 active:bg-canvas-100 dark:active:bg-canvas-800 hover:bg-canvas-50 dark:hover:bg-canvas-850 border border-canvas-200 dark:border-canvas-700 focus:border-accent-500',
  tertiary: 'bg-transparent active:bg-canvas-100 dark:active:bg-canvas-800 hover:bg-canvas-50 dark:hover:bg-canvas-850 border border-transparent focus:border-accent-500',
  destructive: 'bg-status-dangerBg active:bg-status-dangerBorder hover:bg-status-dangerBg/80 border border-status-dangerBorder focus:border-status-dangerText',
};

const DISABLED_SURFACE = 'bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700 opacity-60';

const TEXT_MAP: Record<ButtonVariant, string> = {
  primary: 'text-sm font-semibold text-canvas-white',
  secondary: 'text-sm font-semibold text-canvas-900 dark:text-canvas-50',
  tertiary: 'text-sm font-medium text-accent-600 dark:text-accent-400',
  destructive: 'text-sm font-semibold text-status-dangerText',
};

const DISABLED_TEXT = 'text-sm font-medium text-canvas-400 dark:text-canvas-500';

export function ActionButton({
  label,
  onPress,
  variant = 'primary',
  isSubmitting = false,
  submittingLabel = 'Working…',
  disabled = false,
  disabledReason,
  fullWidth = false,
  accessibilityHint,
}: ActionButtonProps) {
  // Rule R-07: Prevent duplicate submissions while Submitting
  const isLocked = disabled || isSubmitting;
  const widthClass = fullWidth ? 'w-full' : 'self-start';
  const surfaceClass = isLocked ? DISABLED_SURFACE : SURFACE_MAP[variant];
  const textClass = isLocked ? DISABLED_TEXT : TEXT_MAP[variant];

  const buttonElement = (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={isSubmitting ? `${label}, ${submittingLabel}` : label}
      accessibilityHint={accessibilityHint}
      accessibilityState={{ disabled: isLocked, busy: isSubmitting }}
      disabled={isLocked}
      onPress={onPress}
      className={`${widthClass} ${surfaceClass} min-h-[44px] px-4 py-2.5 rounded-md flex-row items-center justify-center`}
    >
      {isSubmitting ? (
        <View className="flex-row items-center">
          <ActivityIndicator size="small" />
          <Text className={`${textClass} ml-2`}>{submittingLabel}</Text>
        </View>
      ) : (
        <Text className={textClass}>{label}</Text>
      )}
    </Pressable>
  );

  if (disabled && disabledReason) {
    return (
      <View className={widthClass}>
        {buttonElement}
        <Text
          accessibilityRole="alert"
          className="text-xs text-canvas-500 dark:text-canvas-400 mt-1 px-1"
        >
          {disabledReason}
        </Text>
      </View>
    );
  }

  return buttonElement;
}
