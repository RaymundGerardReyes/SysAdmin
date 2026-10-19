// Rules: R-08 Platform isolation (iOS Mobile-First Shell), L-01, N-06, N-07
// Enforces >= 44x44pt touch targets, safe-area layout, keyboard avoiding, and secondary overflow sheet
import React, { useState } from 'react';
import {
  SafeAreaView,
  View,
  Text,
  Pressable,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { PlatformShellProps } from './PlatformShell.types';
import { Badge, SurfaceCard } from '../ui';

export function PlatformShell({
  title = 'Pipeline',
  subtitle,
  navItems = [],
  activeNavId,
  onSelectNav,
  sessionStatusLabel = 'Session active',
  sessionAuthenticated = true,
  themeMode = 'system',
  onSelectThemeMode,
  headerActions,
  breadcrumbTrail,
  canNavigateBack = false,
  onNavigateBack,
  children,
}: PlatformShellProps) {
  const [moreMenuOpen, setMoreMenuOpen] = useState(false);

  const primaryTabs = navItems.slice(0, 4);
  const secondaryTabs = navItems.slice(4);
  const isSecondaryActive = secondaryTabs.some((t) => t.id === activeNavId);
  const cycleTheme = () => {
    if (!onSelectThemeMode) return;
    onSelectThemeMode(themeMode === 'dark' ? 'light' : themeMode === 'light' ? 'system' : 'dark');
  };

  return (
    <SafeAreaView className="flex-1 bg-canvas-50 dark:bg-canvas-950">
      <Text className="hidden">iOS Shell</Text>

      {/* Compact Mobile Header */}
      <View className="px-4 py-3 bg-canvas-white dark:bg-canvas-900 border-b border-canvas-200 dark:border-canvas-800 flex-row items-center justify-between">
        <View className="flex-row items-center flex-1 pr-2">
          {canNavigateBack && onNavigateBack && (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Navigate back"
              onPress={onNavigateBack}
              className="min-h-[44px] min-w-[44px] mr-1 items-center justify-center rounded-md active:bg-canvas-100 dark:active:bg-canvas-800"
            >
              <Text className="text-base font-semibold text-accent-600 dark:text-accent-400">
                ‹ Back
              </Text>
            </Pressable>
          )}

          <View className="flex-1">
            {breadcrumbTrail && breadcrumbTrail.length > 0 && (
              <Text
                numberOfLines={1}
                className="text-xs text-canvas-500 dark:text-canvas-400 mb-0.5"
              >
                {breadcrumbTrail.join(' › ')}
              </Text>
            )}
            <Text
              numberOfLines={1}
              className="text-lg font-semibold text-canvas-900 dark:text-canvas-50"
            >
              {title}
            </Text>
            {subtitle ? (
              <Text
                numberOfLines={1}
                className="text-xs text-canvas-500 dark:text-canvas-400"
              >
                {subtitle}
              </Text>
            ) : null}
          </View>
        </View>

        <View className="flex-row items-center gap-2">
          {onSelectThemeMode && (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Cycle color theme"
              onPress={cycleTheme}
              className="min-h-[44px] min-w-[44px] items-center justify-center rounded-md border border-canvas-200 dark:border-canvas-700 bg-canvas-100 dark:bg-canvas-800"
            >
              <Text className="text-sm text-canvas-900 dark:text-canvas-50">
                {themeMode === 'light' ? '☀' : themeMode === 'dark' ? '☾' : '◐'}
              </Text>
            </Pressable>
          )}
          {headerActions}
          <Badge
            label={sessionStatusLabel}
            tone={sessionAuthenticated ? 'success' : 'neutral'}
          />
        </View>
      </View>

      {/* Secondary Navigation Sheet when "More" is tapped */}
      {moreMenuOpen && secondaryTabs.length > 0 && (
        <View className="px-4 pt-3 pb-2 bg-canvas-100 dark:bg-canvas-900 border-b border-canvas-200 dark:border-canvas-800">
          <SurfaceCard variant="elevated" padding="sm">
            <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400 mb-2 px-2">
              Additional Workspaces
            </Text>
            {secondaryTabs.map((item) => {
              const isSelected = item.id === activeNavId;
              return (
                <Pressable
                  key={item.id}
                  accessibilityRole="menuitem"
                  accessibilityLabel={item.label}
                  onPress={() => {
                    setMoreMenuOpen(false);
                    onSelectNav?.(item.id);
                  }}
                  className={`min-h-[44px] px-3 py-2.5 rounded-md flex-row items-center justify-between ${
                    isSelected ? 'bg-accent-50 dark:bg-accent-950' : 'bg-transparent'
                  }`}
                >
                  <Text
                    className={`text-sm ${
                      isSelected
                        ? 'font-semibold text-accent-600 dark:text-accent-400'
                        : 'font-medium text-canvas-900 dark:text-canvas-50'
                    }`}
                  >
                    {item.symbol}  {item.label}
                  </Text>
                </Pressable>
              );
            })}
          </SurfaceCard>
        </View>
      )}

      {/* Single-Column Mobile Scroll Canvas with KeyboardAvoidingView */}
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        className="flex-1"
      >
        <ScrollView
          keyboardShouldPersistTaps="handled"
          className="flex-1 px-4 py-4"
        >
          <View className="pb-6">{children}</View>
        </ScrollView>
      </KeyboardAvoidingView>

      {/* Ergonomic Bottom Tab Bar (Minimum 44x44pt Touch Targets) */}
      {navItems.length > 0 && (
        <View
          accessibilityRole="tablist"
          className="bg-canvas-white dark:bg-canvas-900 border-t border-canvas-200 dark:border-canvas-800 px-2 py-1.5 flex-row items-center justify-around"
        >
          {primaryTabs.map((item) => {
            const isActive = item.id === activeNavId && !moreMenuOpen;
            return (
              <Pressable
                key={item.id}
                accessibilityRole="tab"
                accessibilityLabel={item.label}
                accessibilityState={{ selected: isActive }}
                onPress={() => {
                  setMoreMenuOpen(false);
                  onSelectNav?.(item.id);
                }}
                className={`min-w-[64px] min-h-[44px] px-2.5 py-1.5 rounded-md items-center justify-center ${
                  isActive ? 'bg-accent-50 dark:bg-accent-950' : 'bg-transparent'
                }`}
              >
                <Text
                  className={`text-sm ${
                    isActive
                      ? 'text-accent-600 dark:text-accent-400 font-bold'
                      : 'text-canvas-500 dark:text-canvas-400'
                  }`}
                >
                  {item.symbol}
                </Text>
                <Text
                  className={`text-xs mt-0.5 ${
                    isActive
                      ? 'font-semibold text-accent-600 dark:text-accent-400'
                      : 'font-medium text-canvas-500 dark:text-canvas-400'
                  }`}
                >
                  {item.label}
                </Text>
              </Pressable>
            );
          })}

          {secondaryTabs.length > 0 && (
            <Pressable
              accessibilityRole="tab"
              accessibilityLabel="More workspaces"
              accessibilityState={{ selected: moreMenuOpen || isSecondaryActive }}
              onPress={() => setMoreMenuOpen((prev) => !prev)}
              className={`min-w-[64px] min-h-[44px] px-2.5 py-1.5 rounded-md items-center justify-center ${
                moreMenuOpen || isSecondaryActive
                  ? 'bg-accent-50 dark:bg-accent-950'
                  : 'bg-transparent'
              }`}
            >
              <Text
                className={`text-sm ${
                  moreMenuOpen || isSecondaryActive
                    ? 'text-accent-600 dark:text-accent-400 font-bold'
                    : 'text-canvas-500 dark:text-canvas-400'
                }`}
              >
                •••
              </Text>
              <Text
                className={`text-xs mt-0.5 ${
                  moreMenuOpen || isSecondaryActive
                    ? 'font-semibold text-accent-600 dark:text-accent-400'
                    : 'font-medium text-canvas-500 dark:text-canvas-400'
                }`}
              >
                More
              </Text>
            </Pressable>
          )}
        </View>
      )}
    </SafeAreaView>
  );
}
