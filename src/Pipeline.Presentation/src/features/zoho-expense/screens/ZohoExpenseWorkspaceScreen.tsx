// Rules: R-01 Single bridge door via zohoExpenseApi, R-04 No cross-feature imports, L-04 9-Phase Intent Machine, N-06, N-07
import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { IntegrationWorkspace } from '../../../ui';
import { useMutationIntent } from '../../../intent/useMutationIntent';
import { createCorrelationId } from '../../../core/ids/correlationId';
import {
  CreateZohoExpenseRequest,
  CreateZohoExpenseResponse,
  ZohoExpenseItemContract,
} from '../../../contracts/dtos';
import { ZohoExpenseApi, createZohoExpenseApi } from '../api/zohoExpenseApi';
import { zohoExpenseManifest } from '../feature.manifest';

export interface ZohoExpenseWorkspaceScreenProps {
  readonly api?: ZohoExpenseApi | undefined;
  readonly bridge?: Parameters<typeof createZohoExpenseApi>[0] | undefined;
  readonly canRead?: boolean | undefined;
  readonly canCreate?: boolean | undefined;
  readonly isAuthenticated?: boolean | undefined;
  readonly onSignIn?: (() => void) | undefined;
  readonly onInspectJobs?: (() => void) | undefined;
  readonly onDirtyChange?: ((isDirty: boolean) => void) | undefined;
}

const DEFAULT_ORG_ID = 'zoho_org_primary';
const DEFAULT_MERCHANT = 'Cloudflare Enterprise Edge';
const DEFAULT_CATEGORY = 'Infrastructure & Security';
const DEFAULT_AMOUNT = '650.00';

export function ZohoExpenseWorkspaceScreen({
  api,
  bridge,
  canRead = true,
  canCreate = true,
  isAuthenticated = true,
  onSignIn = () => {},
  onInspectJobs,
  onDirtyChange,
}: ZohoExpenseWorkspaceScreenProps) {
  const zohoExpenseApi = useMemo(() => {
    if (api) return api;
    if (bridge) return createZohoExpenseApi(bridge);
    throw new Error('ZohoExpenseWorkspaceScreen requires either an api or bridge prop');
  }, [api, bridge]);

  const [orgId, setOrgId] = useState(DEFAULT_ORG_ID);
  const [merchant, setMerchant] = useState(DEFAULT_MERCHANT);
  const [category, setCategory] = useState(DEFAULT_CATEGORY);
  const [amount, setAmount] = useState(DEFAULT_AMOUNT);
  const [expenses, setExpenses] = useState<readonly ZohoExpenseItemContract[]>([]);
  const [totalAmount, setTotalAmount] = useState(0);
  const [isSyncing, setIsSyncing] = useState(false);

  const isDirty =
    orgId !== DEFAULT_ORG_ID ||
    merchant !== DEFAULT_MERCHANT ||
    category !== DEFAULT_CATEGORY ||
    amount !== DEFAULT_AMOUNT;

  useEffect(() => {
    onDirtyChange?.(isDirty);
  }, [isDirty, onDirtyChange]);

  const handleSyncExpenses = useCallback(async () => {
    if (!canRead) return;
    setIsSyncing(true);
    const res = await zohoExpenseApi.readExpenses({ orgId }, createCorrelationId());
    setIsSyncing(false);
    if (res.ok) {
      setExpenses(res.value.expenses);
      setTotalAmount(res.value.totalAmount);
    }
  }, [canRead, orgId, zohoExpenseApi]);

  useEffect(() => {
    if (isAuthenticated && canRead && expenses.length === 0) {
      void handleSyncExpenses();
    }
  }, [isAuthenticated, canRead, expenses.length, handleSyncExpenses]);

  const expenseMutation = useMutationIntent<CreateZohoExpenseRequest, CreateZohoExpenseResponse>(
    useCallback(
      async (payload, correlationId, idempotencyKey) => {
        const res = await zohoExpenseApi.createExpense(payload, correlationId, idempotencyKey);
        if (res.ok) {
          setExpenses(res.value.expenses);
          setTotalAmount(
            res.value.expenses.reduce((sum, item) => sum + item.amount, 0)
          );
        }
        return res;
      },
      [zohoExpenseApi]
    ),
    'zoho.expense.create'
  );

  const routes = useMemo(() => zohoExpenseManifest.routes.map((r) => r.name), []);

  return (
    <IntegrationWorkspace
      name={zohoExpenseManifest.name}
      description="Review and manage protected expense workflows through Zoho Expense."
      routes={routes}
      canAccess={isAuthenticated && canRead}
      onSignIn={onSignIn}
      onInspectJobs={onInspectJobs}
      zohoExpenseProps={{
        orgId,
        merchant,
        category,
        amount,
        currency: 'USD',
        expenses,
        totalAmount,
        isSyncing,
        canRead,
        canCreate,
        onChangeOrgId: setOrgId,
        onChangeMerchant: setMerchant,
        onChangeCategory: setCategory,
        onChangeAmount: setAmount,
        onSyncExpenses: () => {
          void handleSyncExpenses();
        },
        onCreateExpense: () => {
          const parsedAmount = Number.parseFloat(amount);
          void expenseMutation.execute(
            {
              orgId: orgId.trim(),
              merchant: merchant.trim(),
              category: category.trim(),
              amount: Number.isFinite(parsedAmount) ? parsedAmount : 0,
              currency: 'USD',
            },
            true
          );
        },
        intentState: expenseMutation.state,
        onConfirmIntent: () => {
          void expenseMutation.confirm();
        },
        onCancelIntent: expenseMutation.reset,
        onResetIntent: expenseMutation.reset,
      }}
    />
  );
}
