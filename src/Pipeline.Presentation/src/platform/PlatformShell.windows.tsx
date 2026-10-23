// Rules: R-08 Platform isolation (Windows Desktop Workspace), L-01, N-06, N-07
import React, { useEffect, useRef } from 'react';
import { View, Text, Pressable, ScrollView, Animated } from 'react-native';
import { PlatformShellProps } from './PlatformShell.types';
import {
  Badge,
  ThemePreference,
  useAppTheme,
  formatThemeOptionLabel,
  getThemeOptionIndex,
  useReducedMotion,
} from '../ui';

const THEME_OPTIONS: readonly {
  readonly mode: ThemePreference;
  readonly label: string;
  readonly glyph: string;
}[] = [
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
  const { isCompact, colorScheme, systemColorScheme } = useAppTheme();
  const reducedMotion = useReducedMotion();

  // Keep sidebar open on medium & expanded screens unless user explicitly collapses it
  const isRailCollapsed = isCompact || !sidebarOpen;

  // Compositor-thread transition for workspace navigation and theme changes
  const contentOpacity = useRef(new Animated.Value(1)).current;
  const contentTranslateY = useRef(new Animated.Value(0)).current;
  const pillScale = useRef(new Animated.Value(1)).current;
  const pillSlide = useRef(new Animated.Value(getThemeOptionIndex(themeMode))).current;

  useEffect(() => {
    const targetIdx = getThemeOptionIndex(themeMode);
    if (reducedMotion) {
      pillSlide.setValue(targetIdx);
      pillScale.setValue(1);
      return;
    }

    pillScale.setValue(0.95);
    Animated.parallel([
      Animated.spring(pillSlide, {
        toValue: targetIdx,
        speed: 28,
        bounciness: 6,
        useNativeDriver: true,
      }),
      Animated.spring(pillScale, {
        toValue: 1,
        speed: 28,
        bounciness: 8,
        useNativeDriver: true,
      }),
    ]).start();
  }, [pillScale, pillSlide, reducedMotion, themeMode]);

  useEffect(() => {
    if (reducedMotion) {
      contentOpacity.setValue(1);
      contentTranslateY.setValue(0);
      return;
    }

    contentOpacity.setValue(0.88);
    contentTranslateY.setValue(6);

    Animated.parallel([
      Animated.timing(contentOpacity, {
        toValue: 1,
        duration: 180,
        useNativeDriver: true,
      }),
      Animated.spring(contentTranslateY, {
        toValue: 0,
        speed: 26,
        bounciness: 5,
        useNativeDriver: true,
      }),
    ]).start();
  }, [activeNavId, colorScheme, contentOpacity, contentTranslateY, reducedMotion]);

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
            <View className="items-center gap-3 min-h-[44px] justify-center">
              <View className="w-9 h-9 rounded-lg bg-accent-600 items-center justify-center border border-accent-500">
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
            <View className="flex-row items-center justify-between px-1.5 min-h-[44px]">
              <View className="flex-row items-center gap-2.5">
                <View className="w-9 h-9 rounded-lg bg-accent-600 items-center justify-center border border-accent-500">
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
              <Text className="text-xs font-mono text-canvas-400 dark:text-canvas-500">
                {colorScheme === 'dark' ? 'DARK' : 'LIGHT'}
              </Text>
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
        {/* Modern Aesthetic Top Command & Title Bar */}
        <View className="bg-canvas-white dark:bg-canvas-900 border-b border-canvas-200 dark:border-canvas-800 px-6 py-3.5 flex-row flex-wrap items-center justify-between gap-4">
          {/* Left Zone: Back Button, Pill Breadcrumbs & Active Workspace Identity */}
          <View className="flex-row items-center gap-3 flex-1 min-w-[260px]">
            {isRailCollapsed && onToggleSidebar && !isCompact && (
              <Pressable
                accessibilityRole="button"
                accessibilityLabel="Expand navigation sidebar"
                onPress={onToggleSidebar}
                className="px-2.5 py-1.5 rounded-lg border border-canvas-200 dark:border-canvas-700 bg-canvas-50 dark:bg-canvas-800 active:bg-canvas-100 dark:active:bg-canvas-700"
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
                className="px-3 py-1.5 rounded-lg border border-canvas-200 dark:border-canvas-700 bg-canvas-50 dark:bg-canvas-800 active:bg-canvas-100 dark:active:bg-canvas-700 focus:border-accent-500"
              >
                <Text className="text-xs font-semibold text-canvas-900 dark:text-canvas-50">
                  ← Overview
                </Text>
              </Pressable>
            )}

            <View className="flex-1">
              <View className="flex-row flex-wrap items-center gap-2">
                {breadcrumbTrail && breadcrumbTrail.length > 0 && (
                  <View className="flex-row items-center gap-1.5 bg-canvas-100 dark:bg-canvas-800 px-2.5 py-1 rounded-md border border-canvas-200 dark:border-canvas-700">
                    {breadcrumbTrail.map((crumb, idx) => (
                      <React.Fragment key={idx}>
                        {idx > 0 && (
                          <Text className="text-xs text-canvas-400 dark:text-canvas-500">›</Text>
                        )}
                        <Text
                          className={`text-xs ${
                            idx === breadcrumbTrail.length - 1
                              ? 'font-semibold text-accent-600 dark:text-accent-400'
                              : 'font-medium text-canvas-500 dark:text-canvas-400'
                          }`}
                        >
                          {crumb}
                        </Text>
                      </React.Fragment>
                    ))}
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
                <View className="flex-row items-center gap-1.5 mt-1">
                  <Text
                    className={`text-xs ${
                      sessionAuthenticated
                        ? 'text-status-successText'
                        : 'text-canvas-400 dark:text-canvas-500'
                    }`}
                  >
                    ●
                  </Text>
                  <Text
                    numberOfLines={1}
                    className="text-xs text-canvas-500 dark:text-canvas-400"
                  >
                    {subtitle}
                  </Text>
                </View>
              ) : null}
            </View>
          </View>

          {/* Right Zone: Animated Segmented Theme Pill & Primary Header Actions */}
          <View className="flex-row flex-wrap items-center gap-3">
            {onSelectThemeMode && (
              <Animated.View
                accessibilityRole="radiogroup"
                accessibilityLabel="Color theme selector"
                style={{ transform: [{ scale: pillScale }] }}
                className="flex-row items-center bg-canvas-100 dark:bg-canvas-800 p-1 rounded-xl border border-canvas-200 dark:border-canvas-700 gap-1"
              >
                {THEME_OPTIONS.map((opt) => {
                  const active = themeMode === opt.mode;
                  const displayLabel = formatThemeOptionLabel(
                    opt.mode,
                    opt.label,
                    systemColorScheme
                  );
                  return (
                    <Pressable
                      key={opt.mode}
                      accessibilityRole="radio"
                      accessibilityLabel={`${opt.label} theme`}
                      accessibilityState={{ checked: active }}
                      onPress={() => onSelectThemeMode(opt.mode)}
                      className={`px-3 py-1.5 rounded-lg flex-row items-center gap-1.5 ${
                        active
                          ? 'bg-accent-600 border border-accent-500'
                          : 'bg-transparent border border-transparent hover:bg-canvas-200 dark:hover:bg-canvas-700'
                      }`}
                    >
                      <Text
                        className={`text-xs ${
                          active
                            ? 'text-canvas-white font-bold'
                            : 'text-canvas-500 dark:text-canvas-400'
                        }`}
                      >
                        {opt.glyph}
                      </Text>
                      {!isCompact && (
                        <Text
                          className={`text-xs ${
                            active
                              ? 'font-semibold text-canvas-white'
                              : 'font-medium text-canvas-600 dark:text-canvas-300'
                          }`}
                        >
                          {displayLabel}
                        </Text>
                      )}
                    </Pressable>
                  );
                })}
              </Animated.View>
            )}

            {headerActions ? (
              <View className="flex-row items-center gap-2">{headerActions}</View>
            ) : null}
          </View>
        </View>

        {/* Constrained, Responsive Content Scroll Region with Compositor Transition */}
        <ScrollView
          className="flex-1"
          contentContainerStyle={{
            flexGrow: 1,
            paddingHorizontal: isCompact ? 16 : 28,
            paddingVertical: isCompact ? 16 : 24,
          }}
        >
          <Animated.View
            style={{
              opacity: contentOpacity,
              transform: [{ translateY: contentTranslateY }],
            }}
            className="max-w-6xl w-full self-center pb-10"
          >
            {children}
          </Animated.View>
        </ScrollView>
      </View>
    </View>
  );
}
