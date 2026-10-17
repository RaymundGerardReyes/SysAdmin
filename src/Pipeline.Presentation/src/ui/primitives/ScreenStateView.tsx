// Rules: R-06 Five states for every screen (loading, success, error, needs-reauth, access-denied), L-01, N-06, N-07
import React from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import { SurfaceCard } from './SurfaceCard';
import { ActionButton } from './ActionButton';
import { Badge } from './Badge';

export type ScreenLifecycleState =
  | 'loading'
  | 'success'
  | 'error'
  | 'needs-reauth'
  | 'access-denied';

export interface ScreenStateViewProps {
  readonly state: ScreenLifecycleState;
  readonly title: string;
  readonly subtitle?: string | undefined;
  readonly errorCode?: string | undefined;
  readonly requiredPermission?: string | undefined;
  readonly onRetry?: (() => void) | undefined;
  readonly onReauthenticate?: (() => void) | undefined;
  readonly children: React.ReactNode;
}

export function ScreenStateView({
  state,
  title,
  subtitle,
  errorCode,
  requiredPermission,
  onRetry,
  onReauthenticate,
  children,
}: ScreenStateViewProps) {
  if (state === 'loading') {
    return (
      <SurfaceCard variant="subtle" padding="lg">
        <View
          accessibilityLiveRegion="polite"
          className="items-center justify-center py-10"
        >
          <ActivityIndicator size="large" />
          <Text className="text-base font-medium text-canvas-900 dark:text-canvas-50 mt-3">
            Loading {title}…
          </Text>
          <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
            Preparing your workspace and verifying access
          </Text>
        </View>
      </SurfaceCard>
    );
  }

  if (state === 'access-denied') {
    return (
      <SurfaceCard variant="elevated" padding="lg">
        <View className="py-4">
          <Badge label="Restricted access" tone="warning" />
          <Text className="text-lg font-semibold text-canvas-900 dark:text-canvas-50 mt-3">
            You don't have permission to access {title}
          </Text>
          <Text className="text-sm text-canvas-500 dark:text-canvas-400 mt-1">
            {requiredPermission
              ? `This workspace requires the "${requiredPermission}" permission. Contact your administrator if you need access.`
              : 'Your current role does not include access to this area.'}
          </Text>
        </View>
      </SurfaceCard>
    );
  }

  if (state === 'needs-reauth') {
    return (
      <SurfaceCard variant="accent" padding="lg">
        <View className="py-4">
          <Badge label="Security verification" tone="accent" />
          <Text className="text-lg font-semibold text-canvas-900 dark:text-canvas-50 mt-3">
            Your session needs to be verified again
          </Text>
          <Text className="text-sm text-canvas-500 dark:text-canvas-400 mt-1 mb-4">
            Please sign in or verify your identity to continue working in {title}.
          </Text>
          {onReauthenticate && (
            <ActionButton
              label="Verify Session"
              onPress={onReauthenticate}
              variant="primary"
            />
          )}
        </View>
      </SurfaceCard>
    );
  }

  if (state === 'error') {
    return (
      <SurfaceCard variant="subtle" padding="lg">
        <View className="py-4">
          <Badge label="Action needed" tone="danger" />
          <Text className="text-lg font-semibold text-canvas-900 dark:text-canvas-50 mt-3">
            We couldn't load {title}
          </Text>
          <Text className="text-sm text-canvas-500 dark:text-canvas-400 mt-1 mb-4">
            {subtitle ?? 'An unexpected problem occurred while loading this workspace.'}
            {errorCode ? ` (Reference: ${errorCode})` : ''}
          </Text>
          {onRetry && (
            <ActionButton label="Try Again" onPress={onRetry} variant="secondary" />
          )}
        </View>
      </SurfaceCard>
    );
  }

  return <View className="flex-1">{children}</View>;
}

