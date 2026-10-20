// Rules: R-08 Platform isolation (Windows Desktop Workspace), L-01, N-06, N-07
import React from 'react';
import { View, Text, Pressable, ScrollView } from 'react-native';
import { PlatformShellProps } from './PlatformShell.types';
import { Badge, ThemePreference, useAppTheme } from '../ui';

const THEME_OPTIONS: readonly { readonly mode: ThemePreference; readonly label: string; readonly glyph: string }[] = [
  { mode: 'light', label: 'Light', glyph: '☀' },
  { mode: 'dark', label: 'Dark', glyph: '☾' },
  { mode: 'system', label: 'Auto', glyph: '◐' },
];

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
  themeMode = 'system',
  onSelectThemeMode,
  headerActions,
  breadcrumbTrail,
  canNavigateBack = false,
  onNavigateBack,
  children,
}: PlatformShellProps) {
  const { isCompact } = useAppTheme();
  // Keep sidebar open on medium & expanded screens unless user explicitly collapses it
  const isRailCollapsed = isCompact || !sidebarOpen;

  return (
    <View className="flex-1 flex-row bg-canvas-50 dark:bg-canvas-950">
      <Text className="hidden">Windows Shell</Text>

      {/* Left Navigation Sidebar */}
      <View
        accessibilityRole="menubar"
        className={`${
          isRailCollapsed ? 'w-16' : 'w-64'
        } bg-canvas-white dark:bg-canvas-900 border-r border-canvas-200 dark:border-canvas-800 flex-col justify-between py-4 px-2.5`}
      >
        <View className="gap-5">
          {/* Workspace Brand Header & Collapse Control */}
          {isRailCollapsed ? (
            <View className="items-center gap-3 min-h-[40px] justify-center">
              <View className="w-8 h-8 rounded-lg bg-accent-600 items-center justify-center">
                <Text className="text-xs font-bold text-canvas-white">P</Text>
              </View>
              {onToggleSidebar && !isCompact && (
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel="Expand navigation sidebar"
                  onPress={onToggleSidebar}
                  className="w-7 h-7 rounded-md items-center justify-center bg-canvas-100 dark:bg-canvas-800 hover:bg-canvas-200 dark:hover:bg-canvas-700 border border-canvas-200 dark:border-canvas-700"
                >
                  <Text className="text-xs font-semibold text-canvas-600 dark:text-canvas-300">»</Text>
                </Pressable>
              )}
            </View>
          ) : (
            <View className="flex-row items-center justify-between px-1.5 min-h-[40px]">
              <View className="flex-row items-center gap-2.5">
                <View className="w-8 h-8 rounded-lg bg-accent-600 items-center justify-center">
                  <Text className="text-xs font-bold text-canvas-white">P</Text>
                </View>
                <View>
                  <Text className="text-sm font-semibold tracking-tight text-canvas-900 dark:text-canvas-50">
                    Pipeline
                  </Text>
                  <Text className="text-xs text-canvas-500 dark:text-canvas-400">
                    Admin Console
                  </Text>
                </View>
              </View>

              {onToggleSidebar && !isCompact && (
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel="Collapse navigation sidebar"
                  onPress={onToggleSidebar}
                  className="w-7 h-7 rounded-md items-center justify-center bg-canvas-100 dark:bg-canvas-800 hover:bg-canvas-200 dark:hover:bg-canvas-700 border border-canvas-200 dark:border-canvas-700"
                >
                  <Text className="text-xs font-semibold text-canvas-600 dark:text-canvas-300">«</Text>
                </Pressable>
              )}
            </View>
          )}

          {/* Navigation Destinations */}
          <View className="gap-1">
            {!isRailCollapsed && (
              <Text className="px-2.5 pb-1 text-xs font-semibold uppercase tracking-wider text-canvas-400 dark:text-canvas-500">
                Workspaces
              </Text>
            )}
            {navItems.map((item) => {
              const isActive = item.id === activeNavId;
              const rowClass = isActive
                ? 'bg-accent-50 dark:bg-accent-950 border border-accent-100 dark:border-accent-900'
                : 'bg-transparent border border-transparent active:bg-canvas-100 dark:active:bg-canvas-800 hover:bg-canvas-50 dark:hover:bg-canvas-800';
              const textClass = isActive
                ? 'text-accent-600 dark:text-accent-400 font-semibold'
                : 'text-canvas-600 dark:text-canvas-300 font-medium';

              return (
                <Pressable
                  key={item.id}
                  accessibilityRole="menuitem"
                  accessibilityLabel={item.label}
                  accessibilityState={{ selected: isActive }}
                  onPress={() => onSelectNav?.(item.id)}
                  className={`${rowClass} min-h-[40px] ${
                    isRailCollapsed ? 'px-0 justify-center' : 'px-3 justify-between'
                  } py-2 rounded-lg flex-row items-center focus:border-accent-500`}
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
                      <Text numberOfLines={1} className={`text-sm ml-3 ${textClass}`}>
                        {item.label}
                      </Text>
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
        <View className="pt-3 border-t border-canvas-200 dark:border-canvas-800 px-1.5">
          {!isRailCollapsed ? (
            <View className="flex-row items-center justify-between">
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
              <Text
                className={`text-xs font-bold ${
                  sessionAuthenticated ? 'text-status-successText' : 'text-canvas-500'
                }`}
              >
                {sessionAuthenticated ? '✓' : '•'}
              </Text>
            </View>
          )}
        </View>
      </View>

      {/* Primary Desktop Workspace */}
      <View className="flex-1 flex-col">
        {/* Minimalistic Aesthetic Top Title & Command Bar */}
        <View className="bg-canvas-white dark:bg-canvas-900 border-b border-canvas-200 dark:border-canvas-800 px-6 py-3 flex-row flex-wrap items-center justify-between gap-4">
          {/* Left: Back Navigation, Breadcrumb & Active Title */}
          <View className="flex-row items-center gap-3 flex-1 min-w-[240px]">
            {isRailCollapsed && onToggleSidebar && !isCompact && (
              <Pressable
                accessibilityRole="button"
                accessibilityLabel="Expand navigation sidebar"
                onPress={onToggleSidebar}
                className="px-2.5 py-1.5 rounded-md border border-canvas-200 dark:border-canvas-700 bg-canvas-50 dark:bg-canvas-800"
              >
                <Text className="text-xs font-semibold text-canvas-900 dark:text-canvas-50">
                  ☰
                </Text>
              </Pressable>
            )}

            {canNavigateBack && onNavigateBack && (
              <Pressable
                accessibilityRole="button"
                accessibilityLabel="Navigate back"
                onPress={onNavigateBack}
                className="px-3 py-1.5 rounded-md border border-canvas-200 dark:border-canvas-700 bg-canvas-50 dark:bg-canvas-800 active:bg-canvas-100 dark:active:bg-canvas-700 focus:border-accent-500"
              >
                <Text className="text-xs font-semibold text-canvas-900 dark:text-canvas-50">
                  ← Back
                </Text>
              </Pressable>
            )}

            <View className="flex-1">
              <View className="flex-row flex-wrap items-center gap-2">
                {breadcrumbTrail && breadcrumbTrail.length > 0 && (
                  <View className="flex-row items-center gap-1.5">
                    {breadcrumbTrail.map((crumb, idx) => (
                      <React.Fragment key={idx}>
                        {idx > 0 && (
                          <Text className="text-xs text-canvas-400 dark:text-canvas-500">/</Text>
                        )}
                        <Text
                          className={`text-xs ${
                            idx === breadcrumbTrail.length - 1
                              ? 'font-medium text-accent-600 dark:text-accent-400'
                              : 'text-canvas-500 dark:text-canvas-400'
                          }`}
                        >
                          {crumb}
                        </Text>
                      </React.Fragment>
                    ))}
                    <Text className="text-xs text-canvas-300 dark:text-canvas-700">•</Text>
                  </View>
                )}
                <Text
                  numberOfLines={1}
                  className="text-base font-semibold tracking-tight text-canvas-900 dark:text-canvas-50"
                >
                  {title}
                </Text>
              </View>

              {subtitle ? (
                <Text
                  numberOfLines={1}
                  className="text-xs text-canvas-500 dark:text-canvas-400 mt-0.5"
                >
                  {subtitle}
                </Text>
              ) : null}
            </View>
          </View>

          {/* Right: Theme Switcher Pill & Primary Header Actions */}
          <View className="flex-row flex-wrap items-center gap-3">
            {onSelectThemeMode && (
              <View
                accessibilityRole="radiogroup"
                accessibilityLabel="Color theme selector"
                className="flex-row items-center bg-canvas-100 dark:bg-canvas-800 p-1 rounded-lg border border-canvas-200 dark:border-canvas-700"
              >
                {THEME_OPTIONS.map((opt) => {
                  const active = themeMode === opt.mode;
                  return (
                    <Pressable
                      key={opt.mode}
                      accessibilityRole="radio"
                      accessibilityLabel={`${opt.label} theme`}
                      accessibilityState={{ checked: active }}
                      onPress={() => onSelectThemeMode(opt.mode)}
                      className={`px-2.5 py-1 rounded-md flex-row items-center gap-1 ${
                        active
                          ? 'bg-canvas-white dark:bg-canvas-900 border border-canvas-200 dark:border-canvas-700'
                          : 'bg-transparent border border-transparent'
                      }`}
                    >
                      <Text
                        className={`text-xs ${
                          active
                            ? 'text-accent-600 dark:text-accent-400 font-bold'
                            : 'text-canvas-500 dark:text-canvas-400'
                        }`}
                      >
                        {opt.glyph}
                      </Text>
                      {!isCompact && (
                        <Text
                          className={`text-xs ${
                            active
                              ? 'font-semibold text-canvas-900 dark:text-canvas-50'
                              : 'font-medium text-canvas-500 dark:text-canvas-400'
                          }`}
                        >
                          {opt.label}
                        </Text>
                      )}
                    </Pressable>
                  );
                })}
              </View>
            )}

            {headerActions ? (
              <View className="flex-row items-center gap-2">{headerActions}</View>
            ) : null}
          </View>
        </View>

        {/* Constrained, Responsive Content Scroll Region */}
        <ScrollView
          className="flex-1"
          contentContainerStyle={{ flexGrow: 1, paddingHorizontal: isCompact ? 16 : 28, paddingVertical: isCompact ? 16 : 24 }}
        >
          <View className="max-w-6xl w-full self-center pb-10">{children}</View>
        </ScrollView>
      </View>
    </View>
  );
}
