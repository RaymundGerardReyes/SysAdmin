// Rules: L-04 9 named intent phases, B-02 No auto-retry on write timeout, R-07 Idempotency preservation, N-06, N-07
import React, { useState } from 'react';
import { View, Text, Pressable } from 'react-native';
import { IntentState } from '../../core/intent/intent.types';
import { SurfaceCard } from './SurfaceCard';
import { Badge, BadgeTone } from './Badge';
import { ActionButton } from './ActionButton';

export interface IntentStatusBannerProps {
  readonly state: IntentState;
  readonly operationSummary?: string;
  readonly onConfirm?: () => void;
  readonly onReset?: () => void;
  readonly onInspectActivity?: () => void;
}

const PHASE_BADGE_MAP: Record<Exclude<IntentState['phase'], 'Idle'>, { label: string; tone: BadgeTone }> = {
  Validating: { label: 'Checking request', tone: 'neutral' },
  AwaitingConfirm: { label: 'Confirmation required', tone: 'accent' },
  Submitting: { label: 'In progress', tone: 'info' },
  AwaitingStepUp: { label: 'Verification needed', tone: 'warning' },
  PendingApproval: { label: 'Pending approval', tone: 'warning' },
  OutcomeUnknown: { label: 'Status unconfirmed', tone: 'warning' },
  Succeeded: { label: 'Completed', tone: 'success' },
  Failed: { label: 'Action needed', tone: 'danger' },
};

export function IntentStatusBanner({
  state,
  operationSummary,
  onConfirm,
  onReset,
  onInspectActivity,
}: IntentStatusBannerProps) {
  const [showTechnicalDetails, setShowTechnicalDetails] = useState(false);

  if (state.phase === 'Idle') {
    return null;
  }

  const badgeMeta = PHASE_BADGE_MAP[state.phase];

  return (
    <View accessibilityLiveRegion="polite" className="mb-4">
      <SurfaceCard
        variant={
          state.phase === 'AwaitingConfirm' || state.phase === 'Succeeded'
            ? 'accent'
            : 'subtle'
        }
        padding="md"
      >
        <View className="flex-row items-center justify-between mb-2">
          <Badge label={badgeMeta.label} tone={badgeMeta.tone} />
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Toggle technical reference details"
            onPress={() => setShowTechnicalDetails((prev) => !prev)}
            className="px-2 py-1"
          >
            <Text className="text-xs font-medium text-canvas-500 dark:text-canvas-400">
              {showTechnicalDetails ? 'Hide reference ▲' : 'Reference details ▼'}
            </Text>
          </Pressable>
        </View>

        {state.phase === 'Validating' && (
          <Text className="text-sm text-canvas-900 dark:text-canvas-50">
            Checking your request before submission…
          </Text>
        )}

        {state.phase === 'AwaitingConfirm' && (
          <View>
            <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
              Please review and confirm this action before it is executed.
            </Text>
            {operationSummary ? (
              <View className="mt-2 p-3 rounded-md bg-canvas-white dark:bg-canvas-900 border border-canvas-200 dark:border-canvas-700">
                <Text className="text-xs font-medium text-canvas-900 dark:text-canvas-50">
                  {operationSummary}
                </Text>
              </View>
            ) : null}
            <View className="flex-row flex-wrap gap-3 mt-3">
              {onConfirm && (
                <ActionButton
                  label="Confirm and Submit"
                  onPress={onConfirm}
                  variant="primary"
                />
              )}
              {onReset && (
                <ActionButton label="Cancel" onPress={onReset} variant="secondary" />
              )}
            </View>
          </View>
        )}

        {state.phase === 'Submitting' && (
          <Text className="text-sm text-canvas-900 dark:text-canvas-50">
            Submitting your request securely. Duplicate submissions are locked while in progress.
          </Text>
        )}

        {state.phase === 'AwaitingStepUp' && (
          <View>
            <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
              Additional identity verification is required to complete this action.
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
              Your request details are preserved so the operation will not be duplicated.
            </Text>
            <View className="flex-row flex-wrap gap-3 mt-3">
              {onConfirm && (
                <ActionButton
                  label="Verify Identity & Continue"
                  onPress={onConfirm}
                  variant="primary"
                />
              )}
              {onReset && (
                <ActionButton label="Cancel" onPress={onReset} variant="secondary" />
              )}
            </View>
          </View>
        )}

        {state.phase === 'PendingApproval' && (
          <View>
            <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
              This request requires secondary approval before it takes effect.
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
              Approval Reference: {state.approvalId || 'Assigned by supervisor queue'}
            </Text>
            <View className="mt-3">
              {onReset && (
                <ActionButton label="Close Notice" onPress={onReset} variant="secondary" />
              )}
            </View>
          </View>
        )}

        {state.phase === 'OutcomeUnknown' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
              The request timed out before confirmation was received.
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
              To prevent duplicate changes, automatic retry is disabled. Please check Recent Activity or Job History before trying again.
            </Text>
            <View className="flex-row flex-wrap gap-3 mt-3">
              {onInspectActivity && (
                <ActionButton
                  label="Inspect Operation History"
                  onPress={onInspectActivity}
                  variant="primary"
                />
              )}
              {onReset && (
                <ActionButton label="Dismiss" onPress={onReset} variant="secondary" />
              )}
            </View>
          </View>
        )}

        {state.phase === 'Succeeded' && (
          <View className="flex-row flex-wrap items-center justify-between gap-2">
            <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
              Changes saved and verified successfully.
            </Text>
            {onReset && (
              <ActionButton label="Dismiss" onPress={onReset} variant="tertiary" />
            )}
          </View>
        )}

        {state.phase === 'Failed' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
              {state.error.message || 'We could not complete this request.'}
            </Text>
            <View className="flex-row flex-wrap gap-3 mt-3">
              {onReset && (
                <ActionButton label="Dismiss" onPress={onReset} variant="secondary" />
              )}
            </View>
          </View>
        )}

        {showTechnicalDetails && (
          <View className="mt-3 pt-3 border-t border-canvas-200 dark:border-canvas-700">
            <Text className="text-xs font-mono text-canvas-500 dark:text-canvas-400">
              Phase: {state.phase} • Correlation ID: {state.correlationId}
              {'idempotencyKey' in state ? ` • Idempotency Key: ${state.idempotencyKey}` : ''}
              {state.phase === 'Failed' ? ` • Error Code: ${state.error.code}` : ''}
            </Text>
          </View>
        )}
      </SurfaceCard>
    </View>
  );
}

