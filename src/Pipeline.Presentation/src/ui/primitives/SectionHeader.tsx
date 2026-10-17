// Rules: L-01 Pure UI layer, N-06, N-07
import React from 'react';
import { View, Text } from 'react-native';

export interface SectionHeaderProps {
  readonly title: string;
  readonly description?: string;
  readonly isPageTitle?: boolean;
  readonly action?: React.ReactNode;
}

export function SectionHeader({
  title,
  description,
  isPageTitle = false,
  action,
}: SectionHeaderProps) {
  return (
    <View className="flex-row flex-wrap items-start justify-between gap-3 mb-4">
      <View className="flex-1 min-w-[220px]">
        <Text
          accessibilityRole="header"
          className={
            isPageTitle
              ? 'text-2xl font-semibold tracking-tight text-canvas-900 dark:text-canvas-50'
              : 'text-lg font-semibold text-canvas-900 dark:text-canvas-50'
          }
        >
          {title}
        </Text>
        {description ? (
          <Text className="text-sm text-canvas-500 dark:text-canvas-400 mt-1">
            {description}
          </Text>
        ) : null}
      </View>
      {action ? <View className="flex-row items-center">{action}</View> : null}
    </View>
  );
}

