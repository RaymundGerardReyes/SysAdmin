// Rules: L-01 Pure UI layer, N-06, N-07
import React from 'react';
import { View, Text } from 'react-native';
import { ActionButton } from './ActionButton';

export interface EmptyStateProps {
  readonly title: string;
  readonly description: string;
  readonly actionLabel?: string;
  readonly onAction?: () => void;
}

export function EmptyState({
  title,
  description,
  actionLabel,
  onAction,
}: EmptyStateProps) {
  return (
    <View className="py-10 px-6 items-center justify-center border border-dashed border-canvas-200 dark:border-canvas-700 rounded-xl bg-canvas-50 dark:bg-canvas-950">
      <Text className="text-base font-semibold text-canvas-900 dark:text-canvas-50 text-center">
        {title}
      </Text>
      <Text className="text-sm text-canvas-500 dark:text-canvas-400 text-center mt-1 max-w-md mb-4">
        {description}
      </Text>
      {actionLabel && onAction ? (
        <ActionButton label={actionLabel} onPress={onAction} variant="secondary" />
      ) : null}
    </View>
  );
}

