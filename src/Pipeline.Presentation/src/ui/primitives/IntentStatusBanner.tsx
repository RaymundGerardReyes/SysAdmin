// Rules: L-04 9 named intent phases, B-02 No auto-retry on write timeout, R-07 Idempotency preservation, N-06, N-07
import React, { useState } from 'react';
import { View, Text, Pressable } from 'react-native';
import { IntentState } from '../../core/intent/intent.types';
import { SurfaceCard } from './SurfaceCard';
import { Badge, BadgeTone } from './Badge';
import { ActionButton } from './ActionButton';

export interface IntentStatusBannerProps {
  readonly state: IntentState;
  readonly operationSummary?: string | undefined;
  readonly onConfirm?: (() => void) | undefined;
  readonly onReset?: (() => void) | undefined;
  readonly onInspectActivity?: (() => void) | undefined;
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
            className="px-2 py-1 rounded-sm active:bg-canvas-100 dark:active:bg-canvas-800"
          >
            <Text className="text-xs font-medium text-canvas-500 dark:text-canvas-400">
              {showTechnicalDetails ? 'Hide reference ▲' : 'Reference details ▼'}
            </Text>
          </Pressable>
        </View>

        {/* 1. Validating */}
        {state.phase === 'Validating' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50 mb-1">
              Validating operation parameters…
            </Text>
            <Text className="text-xs text-canvas-600 dark:text-canvas-300 mb-1">
              Checking request structure, security boundaries, and authorization scopes before dispatch.
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400">
              Please wait momentarily while checks complete.
            </Text>
          </View>
        )}

        {/* 2. AwaitingConfirm */}
        {state.phase === 'AwaitingConfirm' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50 mb-1">
              Review and confirmation required
            </Text>
            <Text className="text-xs text-canvas-600 dark:text-canvas-300 mb-2">
              This action modifies system state or triggers external operations. Changes cannot proceed without explicit operator confirmation.
            </Text>
            {operationSummary ? (
              <View className="mb-3 p-3 rounded-md bg-canvas-white dark:bg-canvas-900 border border-canvas-200 dark:border-canvas-700">
                <Text className="text-xs font-medium text-canvas-900 dark:text-canvas-50">
                  {operationSummary}
                </Text>
              </View>
            ) : null}
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mb-3">
              Review the summary above, then select 'Confirm and Submit' to proceed, or 'Cancel' to safely discard.
            </Text>
            <View className="flex-row flex-wrap gap-3">
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

        {/* 3. Submitting */}
        {state.phase === 'Submitting' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50 mb-1">
              Submitting request securely…
            </Text>
            <Text className="text-xs text-canvas-600 dark:text-canvas-300 mb-1">
              Your request is actively executing on the host. An idempotency reservation is locked to guarantee at-most-once execution.
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400">
              Please wait for host response. Duplicate submissions are disabled while in flight.
            </Text>
          </View>
        )}

        {/* 4. AwaitingStepUp */}
        {state.phase === 'AwaitingStepUp' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50 mb-1">
              Step-up identity verification required
            </Text>
            <Text className="text-xs text-canvas-600 dark:text-canvas-300 mb-1">
              This high-privilege action requires fresh credential verification. Request details are preserved so execution will not be duplicated.
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mb-3">
              Verify your credentials to resume with the same idempotency key, or cancel to abort safely.
            </Text>
            <View className="flex-row flex-wrap gap-3">
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

        {/* 5. PendingApproval */}
        {state.phase === 'PendingApproval' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50 mb-1">
              Operation queued for secondary approval
            </Text>
            <Text className="text-xs text-canvas-600 dark:text-canvas-300 mb-1">
              A supervisor or designated approver must sign off before this transaction executes.
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mb-2">
              Approval Reference: {state.approvalId || 'Assigned by supervisor queue'}
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mb-3">
              You can track approval status in Job History or close this notice.
            </Text>
            <View>
              {onReset && (
                <ActionButton label="Close Notice" onPress={onReset} variant="secondary" />
              )}
            </View>
          </View>
        )}

        {/* 6. OutcomeUnknown */}
        {state.phase === 'OutcomeUnknown' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50 mb-1">
              Request timed out before confirmation
            </Text>
            <Text className="text-xs text-canvas-600 dark:text-canvas-300 mb-1">
              The host did not respond within the deadline. To prevent duplicate changes, automatic retry is strictly disabled (Rule B-02).
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mb-3">
              Inspect Operation History or Job Queue to verify true state before deciding whether to retry.
            </Text>
            <View className="flex-row flex-wrap gap-3">
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

        {/* 7. Succeeded */}
        {state.phase === 'Succeeded' && (
          <View>
            <View className="flex-row flex-wrap items-center justify-between gap-2 mb-1">
              <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                Operation completed successfully
              </Text>
              {onReset && (
                <ActionButton label="Dismiss" onPress={onReset} variant="tertiary" />
              )}
            </View>
            <Text className="text-xs text-canvas-600 dark:text-canvas-300 mb-1">
              All changes were saved, verified, and synchronized with the host.
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400">
              You may continue with further operations or dismiss this notice.
            </Text>
          </View>
        )}

        {/* 8. Failed */}
        {state.phase === 'Failed' && (
          <View>
            <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50 mb-1">
              Operation could not be completed
            </Text>
            <Text className="text-xs text-status-dangerText font-medium mb-1">
              {state.error.message || 'We could not complete this request.'}
            </Text>
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 mb-3">
              The request was rejected without modifying protected data. Review inputs and try again, or dismiss this notice.
            </Text>
            <View className="flex-row flex-wrap gap-3">
              {onReset && (
                <ActionButton label="Dismiss" onPress={onReset} variant="secondary" />
              )}
            </View>
          </View>
        )}

        {/* Toggleable Technical Reference Details */}
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
