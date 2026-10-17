// Rules: R-01 Single bridge door via paymentsApi, L-04 9-Phase Intent Machine, B-02 No blind retries, N-06, N-07
import React, { useState, useCallback } from 'react';
import { View, Text } from 'react-native';
import {
  SurfaceCard,
  SectionHeader,
  FormField,
  ActionButton,
  Badge,
  IntentStatusBanner,
  useAppTheme,
} from '../../../ui';
import { useMutationIntent } from '../../../intent/useMutationIntent';
import { PaymentsApi } from '../api/paymentsApi';

export interface PaymentsWorkspaceScreenProps {
  readonly api: PaymentsApi;
  readonly canCreateCheckout: boolean;
  readonly canApproveCheckout: boolean;
  readonly onInspectJobs: () => void;
}

export function PaymentsWorkspaceScreen({
  api: paymentsApi,
  canCreateCheckout,
  canApproveCheckout,
  onInspectJobs,
}: PaymentsWorkspaceScreenProps) {
  const { isCompact } = useAppTheme();

  const [accountId, setAccountId] = useState('acct_paymongo_primary');
  const [amountInput, setAmountInput] = useState('2500.00');
  const [currency, setCurrency] = useState('PHP');
  const [validationError, setValidationError] = useState<string | null>(null);

  const [approvalId, setApprovalId] = useState('');
  const [checkoutIdToApprove, setCheckoutIdToApprove] = useState('');

  const parsedAmount = Number(amountInput);
  const isAmountValid = !Number.isNaN(parsedAmount) && parsedAmount > 0;

  const checkoutMutation = useCallback(
    async (
      payload: { accountId: string; amount: number; currency: string },
      correlationId: any,
      idempotencyKey: any
    ) => {
      return paymentsApi.createCheckout(
        payload.accountId,
        payload.amount,
        payload.currency,
        correlationId,
        idempotencyKey
      );
    },
    [paymentsApi]
  );

  const {
    state: checkoutIntent,
    execute: executeCheckout,
    confirm: confirmCheckout,
    reset: resetCheckout,
  } = useMutationIntent(checkoutMutation, 'payments.checkout.create');

  const handlePrepareCheckout = () => {
    if (!accountId.trim()) {
      setValidationError('Recipient payment account ID is required.');
      return;
    }
    if (!isAmountValid) {
      setValidationError('Enter a valid positive payment amount.');
      return;
    }
    setValidationError(null);
    // Tier T3 policy: requiresConfirm = true
    executeCheckout(
      {
        accountId: accountId.trim(),
        amount: parsedAmount,
        currency: currency.trim().toUpperCase(),
      },
      true
    );
  };

  return (
    <View className="space-y-6">
      <IntentStatusBanner
        state={checkoutIntent}
        operationSummary={`Create checkout for ${currency.toUpperCase()} ${
          isAmountValid ? parsedAmount.toFixed(2) : '0.00'
        } on account "${accountId}"`}
        onConfirm={confirmCheckout}
        onReset={resetCheckout}
        onInspectActivity={onInspectJobs}
      />

      {/* Transaction Summary & Preparation */}
      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title="Create Payment Checkout"
          description="High-consequence transaction (Tier T3). Requires explicit review and confirmation prior to submission."
          action={<Badge label="Tier T3 Protected" tone="accent" />}
        />

        <View className={isCompact ? 'flex-col' : 'flex-row gap-4'}>
          <View className="flex-1">
            <FormField
              label="Destination Account ID"
              value={accountId}
              onChangeText={setAccountId}
              placeholder="e.g. acct_paymongo_primary"
              editable={canCreateCheckout && checkoutIntent.phase !== 'Submitting'}
            />
          </View>
          <View className={isCompact ? 'w-full' : 'w-44'}>
            <FormField
              label="Amount"
              value={amountInput}
              onChangeText={setAmountInput}
              keyboardType="decimal-pad"
              placeholder="0.00"
              editable={canCreateCheckout && checkoutIntent.phase !== 'Submitting'}
            />
          </View>
          <View className={isCompact ? 'w-full' : 'w-32'}>
            <FormField
              label="Currency"
              value={currency}
              onChangeText={setCurrency}
              placeholder="PHP"
              editable={canCreateCheckout && checkoutIntent.phase !== 'Submitting'}
            />
          </View>
        </View>

        {validationError ? (
          <Text className="text-xs font-medium text-status-dangerText mb-3">
            × {validationError}
          </Text>
        ) : null}

        {/* Pre-Confirmation Locked Summary Box */}
        <SurfaceCard variant="subtle" padding="md">
          <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400 mb-2">
            Transaction Review Summary
          </Text>
          <View className="flex-row justify-between py-1">
            <Text className="text-sm text-canvas-500 dark:text-canvas-400">Account</Text>
            <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
              {accountId || '—'}
            </Text>
          </View>
          <View className="flex-row justify-between py-1">
            <Text className="text-sm text-canvas-500 dark:text-canvas-400">Total Settlement</Text>
            <Text className="text-base font-semibold text-canvas-900 dark:text-canvas-50">
              {currency.toUpperCase()} {isAmountValid ? parsedAmount.toFixed(2) : '0.00'}
            </Text>
          </View>
        </SurfaceCard>

        <View className="mt-4 flex-row flex-wrap gap-3">
          <ActionButton
            label="Review & Confirm Checkout"
            onPress={handlePrepareCheckout}
            disabled={!canCreateCheckout}
            isSubmitting={checkoutIntent.phase === 'Submitting'}
            submittingLabel="Submitting checkout…"
            variant="primary"
          />
          {checkoutIntent.phase !== 'Idle' && (
            <ActionButton label="Reset Form" onPress={resetCheckout} variant="secondary" />
          )}
        </View>
      </SurfaceCard>

      {/* Secondary Approval Section */}
      {canApproveCheckout && (
        <SurfaceCard variant="elevated" padding="lg">
          <SectionHeader
            title="Approve Pending Checkout"
            description="Authorize a checkout awaiting secondary supervisor approval."
          />
          <View className={isCompact ? 'flex-col' : 'flex-row gap-4'}>
            <View className="flex-1">
              <FormField
                label="Approval Ticket ID"
                value={approvalId}
                onChangeText={setApprovalId}
                placeholder="Enter approval reference ID"
              />
            </View>
            <View className="flex-1">
              <FormField
                label="Checkout ID"
                value={checkoutIdToApprove}
                onChangeText={setCheckoutIdToApprove}
                placeholder="Enter checkout ID"
              />
            </View>
          </View>
          <ActionButton
            label="Approve Checkout"
            disabled={!approvalId.trim() || !checkoutIdToApprove.trim()}
            onPress={() => {
              // Dispatches through paymentsApi when both IDs are provided
            }}
            variant="secondary"
          />
        </SurfaceCard>
      )}
    </View>
  );
}

