// Rules: L-01 UI layer, R-03 capability-aware presentation, N-06, N-07
import React from 'react';
import { Text, View } from 'react-native';
import { ActionButton } from './ActionButton';
import { Badge } from './Badge';
import { SectionHeader } from './SectionHeader';
import { SurfaceCard } from './SurfaceCard';

interface IntegrationWorkspaceProps {
  readonly name: string;
  readonly description: string;
  readonly routes: readonly string[];
  readonly canAccess: boolean;
  readonly onSignIn: () => void;
}

export function IntegrationWorkspace({
  name,
  description,
  routes,
  canAccess,
  onSignIn,
}: IntegrationWorkspaceProps) {
  return (
    <SurfaceCard variant="elevated" padding="lg">
      <SectionHeader
        title={name}
        description={description}
        action={<Badge label={canAccess ? 'Available' : 'Sign-in needed'} tone={canAccess ? 'success' : 'neutral'} />}
      />
      <View className="gap-4">
        <View className="p-4 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700">
          <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400 mb-2">
            Registered capabilities
          </Text>
          <Text className="text-sm text-canvas-900 dark:text-canvas-50">
            {routes.join(' • ')}
          </Text>
        </View>
        <Text className="text-sm text-canvas-500 dark:text-canvas-400">
          This workspace is registered and ready for its protected connector operations. Data actions will be enabled after the corresponding API surface is wired.
        </Text>
        {!canAccess ? <ActionButton label="Sign In to Continue" onPress={onSignIn} variant="primary" /> : null}
      </View>
    </SurfaceCard>
  );
}
