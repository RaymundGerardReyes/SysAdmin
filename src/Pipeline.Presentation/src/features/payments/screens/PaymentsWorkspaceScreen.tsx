// Rules: R-01 Single bridge door via paymentsApi, L-04 9-Phase Intent Machine, B-02 No blind retries, N-06, N-07
import React, { useState, useCallback, useMemo, useEffect } from 'react';
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
import { PaymentsApi, createPaymentsApi } from '../api/paymentsApi';

export interface PaymentsWorkspaceScreenProps {
  readonly api?: PaymentsApi | undefined;
  readonly bridge?: Parameters<typeof createPaymentsApi>[0] | undefined;
  readonly canCreateCheckout?: boolean | undefined;
  readonly canApproveCheckout?: boolean | undefined;
  readonly onInspectJobs?: (() => void) | undefined;
  readonly onDirtyChange?: ((isDirty: boolean) => void) | undefined;
}

export function PaymentsWorkspaceScreen({
  api,
  bridge,
  canCreateCheckout = true,
  canApproveCheckout = true,
  onInspectJobs,
  onDirtyChange,
}: PaymentsWorkspaceScreenProps) {
  const { isCompact } = useAppTheme();

  const paymentsApi = useMemo(() => {
    if (api) return api;
    if (bridge) return createPaymentsApi(bridge);
    throw new Error('PaymentsWorkspaceScreen requires either an api or bridge prop');
  }, [api, bridge]);

  const [accountId, setAccountId] = useState('acct_paymongo_primary');
  const [amountInput, setAmountInput] = useState('2500.00');
  const [currency, setCurrency] = useState('PHP');
  const [createValidationError, setCreateValidationError] = useState<string | null>(null);

  const [approvalId, setApprovalId] = useState('');
  const [checkoutIdToApprove, setCheckoutIdToApprove] = useState('');
  const [approveValidationError, setApproveValidationError] = useState<string | null>(null);

  const isCreateDirty = accountId !== 'acct_paymongo_primary' || amountInput !== '2500.00' || currency !== 'PHP';
  const isApproveDirty = Boolean(approvalId.trim() || checkoutIdToApprove.trim());
  const isDirty = isCreateDirty || isApproveDirty;

  useEffect(() => {
    onDirtyChange?.(isDirty);
  }, [isDirty, onDirtyChange]);

  const parsedAmount = Number(amountInput);
  const isAmountValid = !Number.isNaN(parsedAmount) && parsedAmount > 0;

  // 1. Create Checkout Mutation
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

  // 2. Approve Checkout Mutation (Previously inert, now wired)
  const approveMutation = useCallback(
    async (
      payload: { approvalId: string; checkoutId: string },
      correlationId: any,
      idempotencyKey: any
    ) => {
      return paymentsApi.approveCheckout(
        payload.approvalId,
        payload.checkoutId,
        correlationId
      );
    },
    [paymentsApi]
  );

  const {
    state: approveIntent,
    execute: executeApprove,
    confirm: confirmApprove,
    reset: resetApprove,
  } = useMutationIntent(approveMutation, 'payments.checkout.approve');

  const handlePrepareCheckout = () => {
    if (!accountId.trim()) {
      setCreateValidationError('Recipient payment account ID is required.');
      return;
    }
    if (!isAmountValid) {
      setCreateValidationError('Enter a valid positive payment amount.');
      return;
    }
    setCreateValidationError(null);
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

  const handlePrepareApprove = () => {
    if (!approvalId.trim()) {
      setApproveValidationError('Approval ticket ID is required.');
      return;
    }
    if (!checkoutIdToApprove.trim()) {
      setApproveValidationError('Checkout ID is required.');
      return;
    }
    setApproveValidationError(null);
    // Tier T3 policy: requiresConfirm = true
    executeApprove(
      {
        approvalId: approvalId.trim(),
        checkoutId: checkoutIdToApprove.trim(),
      },
      true
    );
  };

  return (
    <View className="gap-6">
      <IntentStatusBanner
        state={checkoutIntent}
        operationSummary={`Create checkout for ${currency.toUpperCase()} ${
          isAmountValid ? parsedAmount.toFixed(2) : '0.00'
        } on account "${accountId}"`}
        onConfirm={confirmCheckout}
        onReset={resetCheckout}
        onInspectActivity={onInspectJobs}
      />

      <IntentStatusBanner
        state={approveIntent}
        operationSummary={`Approve and disburse checkout "${checkoutIdToApprove}" with approval ticket "${approvalId}"`}
        onConfirm={confirmApprove}
        onReset={resetApprove}
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
              required
              onClear={() => setAccountId('')}
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
              required
              onClear={() => setAmountInput('')}
            />
          </View>
          <View className={isCompact ? 'w-full' : 'w-32'}>
            <FormField
              label="Currency"
              value={currency}
              onChangeText={setCurrency}
              placeholder="PHP"
              editable={canCreateCheckout && checkoutIntent.phase !== 'Submitting'}
              required
              onClear={() => setCurrency('')}
            />
          </View>
        </View>

        {createValidationError ? (
          <Text className="text-xs font-medium text-status-dangerText mb-3">
            × {createValidationError}
          </Text>
        ) : null}

        {/* Pre-Confirmation Locked Summary Box with Consequence Notice */}
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
          <Text className="text-xs text-status-warningText mt-2">
            Notice: Submitting will reserve settlement funds. Tier T3 requires confirmation before execution.
          </Text>
        </SurfaceCard>

        <View className="mt-4 flex-row flex-wrap gap-3">
          <ActionButton
            label="Review & Confirm Checkout"
            onPress={handlePrepareCheckout}
            disabled={!canCreateCheckout}
            disabledReason={!canCreateCheckout ? 'Missing payments.checkout.create permission' : undefined}
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
            description="Authorize a checkout awaiting secondary supervisor approval (Tier T3). Permanently releases payment settlement."
            action={<Badge label="Tier T3 Mutation" tone="warning" />}
          />
          <View className={isCompact ? 'flex-col' : 'flex-row gap-4'}>
            <View className="flex-1">
              <FormField
                label="Approval Ticket ID"
                value={approvalId}
                onChangeText={setApprovalId}
                placeholder="Enter approval reference ID"
                required
                onClear={() => setApprovalId('')}
                editable={approveIntent.phase !== 'Submitting'}
              />
            </View>
            <View className="flex-1">
              <FormField
                label="Checkout ID"
                value={checkoutIdToApprove}
                onChangeText={setCheckoutIdToApprove}
                placeholder="Enter checkout ID"
                required
                onClear={() => setCheckoutIdToApprove('')}
                editable={approveIntent.phase !== 'Submitting'}
              />
            </View>
          </View>

          {approveValidationError ? (
            <Text className="text-xs font-medium text-status-dangerText mb-3">
              × {approveValidationError}
            </Text>
          ) : null}

          <View className="mb-4">
            <SurfaceCard variant="subtle" padding="sm">
              <Text className="text-xs text-status-dangerText font-medium">
                Consequence: Approving this checkout will immediately authorize settlement and debit funds from the destination account. This action cannot be reversed.
              </Text>
            </SurfaceCard>
          </View>

          <View className="flex-row flex-wrap gap-3">
            <ActionButton
              label="Review & Approve Checkout"
              disabled={!approvalId.trim() || !checkoutIdToApprove.trim() || !canApproveCheckout}
              disabledReason={
                !canApproveCheckout
                  ? 'Missing payments.checkout.approve permission'
                  : !approvalId.trim() || !checkoutIdToApprove.trim()
                  ? 'Enter both Approval Ticket ID and Checkout ID'
                  : undefined
              }
              onPress={handlePrepareApprove}
              isSubmitting={approveIntent.phase === 'Submitting'}
              submittingLabel="Approving checkout…"
              variant="primary"
            />
            {approveIntent.phase !== 'Idle' && (
              <ActionButton label="Reset Approval Form" onPress={resetApprove} variant="secondary" />
            )}
          </View>
        </SurfaceCard>
      )}
    </View>
  );
}
