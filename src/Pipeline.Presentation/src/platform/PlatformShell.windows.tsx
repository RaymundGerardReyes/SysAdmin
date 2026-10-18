// Rules: R-08 Platform isolation (Windows Desktop Workspace), L-01, N-06, N-07
import React from 'react';
import { View, Text, Pressable, ScrollView } from 'react-native';
import { PlatformShellProps } from './PlatformShell.types';
import { Badge, useAppTheme } from '../ui';

export function PlatformShell({
  title = 'Operations Workspace',
  subtitle,
  navItems = [],
  activeNavId,
  onSelectNav,
  sidebarOpen = true,
  onToggleSidebar,
  sessionStatusLabel = 'Session active',
  sessionAuthenticated = true,
  headerActions,
  breadcrumbTrail,
  canNavigateBack = false,
  onNavigateBack,
  children,
}: PlatformShellProps) {
  const { isCompact, isMedium } = useAppTheme();
  const isRailCollapsed = isCompact || isMedium || !sidebarOpen;

  return (
    <View className="flex-1 flex-row bg-canvas-50 dark:bg-canvas-950">
      <Text className="hidden">Windows Shell</Text>

      {/* Left Navigation Sidebar */}
      <View
        accessibilityRole="menubar"
        className={`${
          isRailCollapsed ? 'w-16' : 'w-60'
        } bg-canvas-white dark:bg-canvas-900 border-r border-canvas-200 dark:border-canvas-800 flex-col justify-between py-5 px-2.5`}
      >
        <View>
          {/* Workspace Identity & Collapse Control */}
          <View className="flex-row items-center justify-between px-2 mb-6">
            {!isRailCollapsed ? (
              <View>
                <Text className="text-base font-semibold tracking-tight text-canvas-900 dark:text-canvas-50">
                  Pipeline
                </Text>
                <Text className="text-xs text-canvas-500 dark:text-canvas-400">
                  Operations Workspace
                </Text>
              </View>
            ) : (
              <View className="w-9 h-9 rounded-md bg-accent-50 dark:bg-accent-950 border border-accent-100 dark:border-accent-900 items-center justify-center">
                <Text className="text-sm font-semibold text-accent-600 dark:text-accent-400">
                  P
                </Text>
              </View>
            )}

            {onToggleSidebar && !isCompact && (
              <Pressable
                accessibilityRole="button"
                accessibilityLabel={isRailCollapsed ? 'Expand navigation sidebar' : 'Collapse navigation sidebar'}
                onPress={onToggleSidebar}
                className="min-w-[36px] min-h-[36px] rounded-md items-center justify-center bg-canvas-100 dark:bg-canvas-800 focus:border-accent-500 border border-transparent"
              >
                <Text className="text-xs font-semibold text-canvas-900 dark:text-canvas-50">
                  {isRailCollapsed ? '»' : '«'}
                </Text>
              </Pressable>
            )}
          </View>

          {/* Navigation Destinations */}
          <View className="space-y-1">
            {navItems.map((item) => {
              const isActive = item.id === activeNavId;
              const rowClass = isActive
                ? 'bg-accent-50 dark:bg-accent-950 border border-accent-100 dark:border-accent-900'
                : 'bg-transparent border border-transparent active:bg-canvas-100 dark:active:bg-canvas-800 hover:bg-canvas-50 dark:hover:bg-canvas-800';
              const textClass = isActive
                ? 'text-accent-600 dark:text-accent-400 font-semibold'
                : 'text-canvas-900 dark:text-canvas-50 font-medium';

              return (
                <Pressable
                  key={item.id}
                  accessibilityRole="menuitem"
                  accessibilityLabel={item.label}
                  accessibilityState={{ selected: isActive }}
                  onPress={() => onSelectNav?.(item.id)}
                  className={`${rowClass} min-h-[44px] px-3 py-2 rounded-md flex-row items-center justify-between focus:border-accent-500`}
                >
                  <View className="flex-row items-center">
                    <Text
                      className={`text-base ${
                        isActive
                          ? 'text-accent-600 dark:text-accent-400'
                          : 'text-canvas-500 dark:text-canvas-400'
                      }`}
                    >
                      {item.symbol}
                    </Text>
                    {!isRailCollapsed && (
                      <Text className={`text-sm ml-3 ${textClass}`}>{item.label}</Text>
                    )}
                  </View>

                  {!isRailCollapsed && item.badgeCount !== undefined && item.badgeCount > 0 ? (
                    <Badge label={String(item.badgeCount)} tone="accent" showSymbol={false} />
                  ) : null}
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Session Status Footer */}
        <View className="pt-4 border-t border-canvas-200 dark:border-canvas-800 px-2">
          {!isRailCollapsed ? (
            <View className="space-y-1">
              <Badge
                label={sessionStatusLabel}
                tone={sessionAuthenticated ? 'success' : 'neutral'}
              />
            </View>
          ) : (
            <View
              accessibilityLabel={sessionStatusLabel}
              className="items-center justify-center py-1"
            >
              <Text className="text-xs font-bold text-canvas-500">
                {sessionAuthenticated ? '✓' : '•'}
              </Text>
            </View>
          )}
        </View>
      </View>

      {/* Primary Desktop Workspace */}
      <View className="flex-1 flex-col">
        {/* Restrained Top Command Header */}
        <View className="min-h-[64px] bg-canvas-white dark:bg-canvas-900 border-b border-canvas-200 dark:border-canvas-800 px-6 py-3 flex-row flex-wrap items-center justify-between gap-4">
          <View className="flex-row items-center">
            {canNavigateBack && onNavigateBack && (
              <Pressable
                accessibilityRole="button"
                accessibilityLabel="Navigate back"
                onPress={onNavigateBack}
                className="mr-3 px-3 py-1.5 rounded-md border border-canvas-200 dark:border-canvas-700 bg-canvas-50 dark:bg-canvas-800 active:bg-canvas-100 dark:active:bg-canvas-700 focus:border-accent-500"
              >
                <Text className="text-xs font-semibold text-canvas-900 dark:text-canvas-50">
                  ← Back
                </Text>
              </Pressable>
            )}

            <View>
              {breadcrumbTrail && breadcrumbTrail.length > 0 && (
                <View className="flex-row items-center gap-1.5 mb-0.5">
                  {breadcrumbTrail.map((crumb, idx) => (
                    <React.Fragment key={idx}>
                      {idx > 0 && (
                        <Text className="text-xs text-canvas-400 dark:text-canvas-500">/</Text>
                      )}
                      <Text
                        className={`text-xs ${
                          idx === breadcrumbTrail.length - 1
                            ? 'font-semibold text-accent-600 dark:text-accent-400'
                            : 'text-canvas-500 dark:text-canvas-400'
                        }`}
                      >
                        {crumb}
                      </Text>
                    </React.Fragment>
                  ))}
                </View>
              )}
              <Text className="text-lg font-semibold text-canvas-900 dark:text-canvas-50">
                {title}
              </Text>
              {subtitle ? (
                <Text className="text-xs text-canvas-500 dark:text-canvas-400">
                  {subtitle}
                </Text>
              ) : null}
            </View>
          </View>
          {headerActions ? <View className="flex-row items-center gap-2">{headerActions}</View> : null}
        </View>

        {/* Constrained, Readable Content Region */}
        <ScrollView className="flex-1 px-6 py-6">
          <View className="max-w-5xl w-full self-center pb-8">{children}</View>
        </ScrollView>
      </View>
    </View>
  );
}
