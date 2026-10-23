// Rules: L-01 UI layer, L-04 9 named intent phases, R-03 capability-aware presentation, N-06, N-07
import React, { useEffect, useRef } from 'react';
import { Animated, Easing, Text, View } from 'react-native';
import {
  BiginContactItemContract,
  BiginModuleItemResponse,
  BiginNotificationDetailsContract,
  BiginOrgDetailsContract,
  ChatMessageContract,
  ZohoExpenseItemContract,
} from '../../contracts/dtos';
import { IntentState } from '../../core/intent/intent.types';
import { useReducedMotion } from '../theme/useMotion';
import { ActionButton } from './ActionButton';
import { Badge } from './Badge';
import { FormField } from './FormField';
import { IntentStatusBanner } from './IntentStatusBanner';
import { SectionHeader } from './SectionHeader';
import { SurfaceCard } from './SurfaceCard';

export interface BiginWorkspaceData {
  readonly org?: BiginOrgDetailsContract | undefined;
  readonly modules: readonly BiginModuleItemResponse[];
  readonly watchChannels: readonly BiginNotificationDetailsContract[];
  readonly contacts: readonly BiginContactItemContract[];
  readonly isSyncing: boolean;
  readonly canRead: boolean;
  readonly canCreate: boolean;
  readonly fullName: string;
  readonly email: string;
  readonly accountName: string;
  readonly title: string;
  readonly onChangeFullName: (value: string) => void;
  readonly onChangeEmail: (value: string) => void;
  readonly onChangeAccountName: (value: string) => void;
  readonly onChangeTitle: (value: string) => void;
  readonly onSync: () => void;
  readonly onCreateContact: () => void;
  readonly intentState: IntentState;
  readonly onConfirmIntent: () => void;
  readonly onCancelIntent: () => void;
  readonly onResetIntent: () => void;
}

export interface ChatWorkspaceData {
  readonly spaceId: string;
  readonly threadKey: string;
  readonly messageText: string;
  readonly messages: readonly ChatMessageContract[];
  readonly canPost: boolean;
  readonly onChangeSpaceId: (value: string) => void;
  readonly onChangeThreadKey: (value: string) => void;
  readonly onChangeMessageText: (value: string) => void;
  readonly onSendMessage: () => void;
  readonly intentState: IntentState;
  readonly onConfirmIntent: () => void;
  readonly onCancelIntent: () => void;
  readonly onResetIntent: () => void;
}

export interface ZohoExpenseWorkspaceData {
  readonly orgId: string;
  readonly merchant: string;
  readonly category: string;
  readonly amount: string;
  readonly currency: string;
  readonly expenses: readonly ZohoExpenseItemContract[];
  readonly totalAmount: number;
  readonly isSyncing: boolean;
  readonly canRead: boolean;
  readonly canCreate: boolean;
  readonly onChangeOrgId: (value: string) => void;
  readonly onChangeMerchant: (value: string) => void;
  readonly onChangeCategory: (value: string) => void;
  readonly onChangeAmount: (value: string) => void;
  readonly onSyncExpenses: () => void;
  readonly onCreateExpense: () => void;
  readonly intentState: IntentState;
  readonly onConfirmIntent: () => void;
  readonly onCancelIntent: () => void;
  readonly onResetIntent: () => void;
}

export interface IntegrationWorkspaceProps {
  readonly name: string;
  readonly description: string;
  readonly routes: readonly string[];
  readonly canAccess: boolean;
  readonly onSignIn: () => void;
  readonly biginProps?: BiginWorkspaceData | undefined;
  readonly chatProps?: ChatWorkspaceData | undefined;
  readonly zohoExpenseProps?: ZohoExpenseWorkspaceData | undefined;
  readonly onInspectJobs?: (() => void) | undefined;
}

export function IntegrationWorkspace({
  name,
  description,
  routes,
  canAccess,
  onSignIn,
  biginProps,
  chatProps,
  zohoExpenseProps,
  onInspectJobs,
}: IntegrationWorkspaceProps) {
  const reducedMotion = useReducedMotion();
  const isTestEnv = typeof process !== 'undefined' && process.env['NODE_ENV'] === 'test';
  const disableMotion = reducedMotion || isTestEnv;

  const listOpacity = useRef(new Animated.Value(1)).current;
  const listTranslateY = useRef(new Animated.Value(0)).current;

  const itemCount =
    (biginProps?.contacts.length ?? 0) +
    (chatProps?.messages.length ?? 0) +
    (zohoExpenseProps?.expenses.length ?? 0);

  useEffect(() => {
    if (disableMotion) {
      listOpacity.setValue(1);
      listTranslateY.setValue(0);
      return;
    }

    listOpacity.setValue(0.35);
    listTranslateY.setValue(6);

    const anim = Animated.parallel([
      Animated.timing(listOpacity, {
        toValue: 1,
        duration: 210,
        easing: Easing.out(Easing.cubic),
        useNativeDriver: true,
      }),
      Animated.timing(listTranslateY, {
        toValue: 0,
        duration: 210,
        easing: Easing.out(Easing.cubic),
        useNativeDriver: true,
      }),
    ]);

    anim.start();
    return () => {
      anim.stop();
    };
  }, [itemCount, disableMotion, listOpacity, listTranslateY]);

  return (
    <View className="gap-5">
      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title={name}
          description={description}
          action={
            <Badge
              label={canAccess ? 'Protected Connector Active' : 'Sign-in needed'}
              tone={canAccess ? 'success' : 'neutral'}
            />
          }
        />
        <View className="gap-4">
          <View className="p-3.5 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700 flex-row flex-wrap items-center justify-between gap-2">
            <View>
              <Text className="text-xs font-semibold uppercase tracking-wider text-canvas-500 dark:text-canvas-400">
                Registered Capability Routes & Pipeline.Contracts Surface
              </Text>
              <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50 mt-1">
                {routes.join(' • ')}
              </Text>
            </View>
            {!canAccess ? (
              <ActionButton label="Sign In to Continue" onPress={onSignIn} variant="primary" />
            ) : null}
          </View>
        </View>
      </SurfaceCard>

      {/* 1. ZOHO BIGIN INTERACTIVE WORKSPACE */}
      {biginProps ? (
        <View className="gap-5">
          {biginProps.org ? (
            <SurfaceCard variant="subtle" padding="md">
              <SectionHeader
                title="Zoho Bigin Organization & Webhook Watch"
                description="Live Pipeline.Contracts BiginOrgDetailsContract, BiginModuleItemResponse, and BiginNotificationDetailsContract telemetry."
                action={
                  <ActionButton
                    label={biginProps.isSyncing ? 'Syncing Bigin...' : 'Sync Contacts & Watch'}
                    onPress={biginProps.onSync}
                    isSubmitting={biginProps.isSyncing}
                    submittingLabel="Syncing Bigin..."
                    disabled={!biginProps.canRead}
                    disabledReason={!biginProps.canRead ? 'Missing bigin.contacts.read' : undefined}
                    variant="secondary"
                  />
                }
              />
              <View className="flex-row flex-wrap gap-3 mb-3">
                <View className="flex-1 min-w-[200px] p-3 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700">
                  <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400">
                    Organization ({biginProps.org.id})
                  </Text>
                  <Text className="text-sm font-bold text-canvas-900 dark:text-canvas-50 mt-1">
                    {biginProps.org.companyName}
                  </Text>
                  <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-0.5">
                    {biginProps.org.paidType} • {biginProps.org.city}, {biginProps.org.country} ({biginProps.org.currencySymbol}{' '}
                    {biginProps.org.currency})
                  </Text>
                </View>
                <View className="flex-1 min-w-[200px] p-3 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700">
                  <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400">
                    Enabled Modules ({biginProps.modules.length})
                  </Text>
                  <View className="flex-row flex-wrap gap-1.5 mt-1.5">
                    {biginProps.modules.map((mod) => (
                      <Badge
                        key={mod.id}
                        label={`${mod.moduleName} (${mod.apiName})`}
                        tone="info"
                        showSymbol={false}
                      />
                    ))}
                  </View>
                </View>
                <View className="flex-1 min-w-[200px] p-3 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700">
                  <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400">
                    Active Watch Channels ({biginProps.watchChannels.length})
                  </Text>
                  {biginProps.watchChannels.map((ch) => (
                    <Text key={ch.channelId} className="text-xs font-mono text-canvas-700 dark:text-canvas-300 mt-1">
                      #{ch.channelId} → {ch.resourceName} ({(ch.events ?? []).join(', ')})
                    </Text>
                  ))}
                </View>
              </View>
            </SurfaceCard>
          ) : null}

          <SurfaceCard variant="elevated" padding="lg">
            <SectionHeader
              title="Create Zoho Bigin Contact (T2 Mutation)"
              description="Dispatches bigin.contacts.create through UseCaseDispatcher with idempotency and confirmation."
            />
            <View className="flex-row flex-wrap gap-4">
              <View className="flex-1 min-w-[220px]">
                <FormField
                  label="Contact Full Name"
                  value={biginProps.fullName}
                  onChangeText={biginProps.onChangeFullName}
                  placeholder="e.g. Sophia Reyes"
                  required
                />
              </View>
              <View className="flex-1 min-w-[220px]">
                <FormField
                  label="Work Email"
                  value={biginProps.email}
                  onChangeText={biginProps.onChangeEmail}
                  placeholder="s.reyes@partner-enterprise.io"
                  keyboardType="email-address"
                  required
                />
              </View>
            </View>
            <View className="flex-row flex-wrap gap-4">
              <View className="flex-1 min-w-[220px]">
                <FormField
                  label="Account / Company Name"
                  value={biginProps.accountName}
                  onChangeText={biginProps.onChangeAccountName}
                  placeholder="Partner Enterprise Holdings"
                  required
                />
              </View>
              <View className="flex-1 min-w-[220px]">
                <FormField
                  label="Role / Title"
                  value={biginProps.title}
                  onChangeText={biginProps.onChangeTitle}
                  placeholder="Director of Settlements"
                />
              </View>
            </View>
            <ActionButton
              label="Create Bigin Contact"
              onPress={biginProps.onCreateContact}
              variant="primary"
              isSubmitting={biginProps.intentState.phase === 'Submitting' || biginProps.intentState.phase === 'Validating'}
              submittingLabel="Creating contact..."
              disabled={!biginProps.canCreate}
              disabledReason={!biginProps.canCreate ? 'Missing bigin.contacts.create permission' : undefined}
            />
            <IntentStatusBanner
              state={biginProps.intentState}
              operationSummary={`Add "${biginProps.fullName || 'Contact'}" (${biginProps.email || 'email'}) under account "${biginProps.accountName || 'Account'}" in Zoho Bigin.`}
              onConfirm={biginProps.onConfirmIntent}
              onReset={biginProps.onResetIntent}
              onInspectActivity={onInspectJobs}
            />
          </SurfaceCard>

          <SurfaceCard variant="subtle" padding="lg">
            <SectionHeader
              title={`Protected Bigin Contacts (${biginProps.contacts.length})`}
              description="Synchronized Contact records via bigin.contacts.read."
            />
            <Animated.View
              style={{
                opacity: listOpacity,
                transform: [{ translateY: listTranslateY }],
              }}
              className="gap-2.5"
            >
              {biginProps.contacts.map((contact) => (
                <View
                  key={contact.id}
                  className="p-3.5 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700 flex-row flex-wrap items-center justify-between gap-2"
                >
                  <View>
                    <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                      {contact.fullName}{' '}
                      <Text className="text-xs font-normal text-canvas-500 dark:text-canvas-400">
                        • {contact.title}
                      </Text>
                    </Text>
                    <Text className="text-xs text-canvas-600 dark:text-canvas-300 mt-0.5">
                      {contact.accountName} • {contact.email}
                    </Text>
                  </View>
                  <Badge label={contact.id} tone="neutral" showSymbol={false} />
                </View>
              ))}
            </Animated.View>
          </SurfaceCard>
        </View>
      ) : null}

      {/* 2. GOOGLE CHAT INTERACTIVE WORKSPACE */}
      {chatProps ? (
        <View className="gap-5">
          <SurfaceCard variant="elevated" padding="lg">
            <SectionHeader
              title="Broadcast Protected Space Message (T2 Mutation)"
              description="Dispatches chat.create to a pinned Google Chat space with capability ticket verification."
            />
            <View className="flex-row flex-wrap gap-4">
              <View className="flex-1 min-w-[220px]">
                <FormField
                  label="Target Space ID"
                  value={chatProps.spaceId}
                  onChangeText={chatProps.onChangeSpaceId}
                  placeholder="spaces/ops-alerts"
                  required
                />
              </View>
              <View className="flex-1 min-w-[220px]">
                <FormField
                  label="Thread Key"
                  value={chatProps.threadKey}
                  onChangeText={chatProps.onChangeThreadKey}
                  placeholder="ops-incident-bridge"
                />
              </View>
            </View>
            <FormField
              label="Operational Message Payload"
              value={chatProps.messageText}
              onChangeText={chatProps.onChangeMessageText}
              placeholder="Enter operational status broadcast for Google Chat..."
              multiline
              required
            />
            <ActionButton
              label="Post to Google Chat Space"
              onPress={chatProps.onSendMessage}
              variant="primary"
              isSubmitting={chatProps.intentState.phase === 'Submitting' || chatProps.intentState.phase === 'Validating'}
              submittingLabel="Posting message..."
              disabled={!chatProps.canPost}
              disabledReason={!chatProps.canPost ? 'Missing chat.messages.create permission' : undefined}
            />
            <IntentStatusBanner
              state={chatProps.intentState}
              operationSummary={`Post operational announcement to Google Chat space "${chatProps.spaceId}" (thread "${chatProps.threadKey || 'default'}").`}
              onConfirm={chatProps.onConfirmIntent}
              onReset={chatProps.onResetIntent}
              onInspectActivity={onInspectJobs}
            />
          </SurfaceCard>

          <SurfaceCard variant="subtle" padding="lg">
            <SectionHeader
              title={`Recent Space Broadcasts (${chatProps.messages.length})`}
              description="Verified operational dispatches across Google Chat spaces."
            />
            <Animated.View
              style={{
                opacity: listOpacity,
                transform: [{ translateY: listTranslateY }],
              }}
              className="gap-2.5"
            >
              {chatProps.messages.map((msg) => (
                <View
                  key={msg.messageId}
                  className="p-3.5 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700 gap-1"
                >
                  <View className="flex-row items-center justify-between flex-wrap gap-2">
                    <Text className="text-xs font-semibold text-accent-600 dark:text-accent-400">
                      {msg.spaceId} • #{msg.threadKey}
                    </Text>
                    <Text className="text-xs font-mono text-canvas-500 dark:text-canvas-400">
                      {msg.sender}
                    </Text>
                  </View>
                  <Text className="text-sm text-canvas-900 dark:text-canvas-50 mt-1">
                    {msg.text}
                  </Text>
                </View>
              ))}
            </Animated.View>
          </SurfaceCard>
        </View>
      ) : null}

      {/* 3. ZOHO EXPENSE INTERACTIVE WORKSPACE */}
      {zohoExpenseProps ? (
        <View className="gap-5">
          <SurfaceCard variant="elevated" padding="lg">
            <SectionHeader
              title="Submit Protected Expense Entry (T2 Mutation)"
              description="Dispatches zoho.expense.create with idempotency protection and organization scope checks."
              action={
                <ActionButton
                  label={zohoExpenseProps.isSyncing ? 'Refreshing...' : 'Refresh Ledger'}
                  onPress={zohoExpenseProps.onSyncExpenses}
                  isSubmitting={zohoExpenseProps.isSyncing}
                  submittingLabel="Refreshing..."
                  disabled={!zohoExpenseProps.canRead}
                  disabledReason={!zohoExpenseProps.canRead ? 'Missing zoho.expense.read' : undefined}
                  variant="secondary"
                />
              }
            />
            <View className="flex-row flex-wrap gap-4">
              <View className="flex-1 min-w-[200px]">
                <FormField
                  label="Merchant / Vendor"
                  value={zohoExpenseProps.merchant}
                  onChangeText={zohoExpenseProps.onChangeMerchant}
                  placeholder="AWS Cloud Services"
                  required
                />
              </View>
              <View className="flex-1 min-w-[200px]">
                <FormField
                  label="Expense Category"
                  value={zohoExpenseProps.category}
                  onChangeText={zohoExpenseProps.onChangeCategory}
                  placeholder="Infrastructure & Security"
                  required
                />
              </View>
              <View className="w-40 min-w-[140px]">
                <FormField
                  label={`Amount (${zohoExpenseProps.currency})`}
                  value={zohoExpenseProps.amount}
                  onChangeText={zohoExpenseProps.onChangeAmount}
                  placeholder="650.00"
                  keyboardType="decimal-pad"
                  required
                />
              </View>
            </View>
            <ActionButton
              label="Submit Expense Entry"
              onPress={zohoExpenseProps.onCreateExpense}
              variant="primary"
              isSubmitting={
                zohoExpenseProps.intentState.phase === 'Submitting' ||
                zohoExpenseProps.intentState.phase === 'Validating'
              }
              submittingLabel="Submitting expense..."
              disabled={!zohoExpenseProps.canCreate}
              disabledReason={!zohoExpenseProps.canCreate ? 'Missing zoho.expense.create permission' : undefined}
            />
            <IntentStatusBanner
              state={zohoExpenseProps.intentState}
              operationSummary={`Submit ${zohoExpenseProps.currency} ${zohoExpenseProps.amount || '0.00'} expense for "${zohoExpenseProps.merchant || 'Vendor'}" (${zohoExpenseProps.category}) in organization ${zohoExpenseProps.orgId}.`}
              onConfirm={zohoExpenseProps.onConfirmIntent}
              onReset={zohoExpenseProps.onResetIntent}
              onInspectActivity={onInspectJobs}
            />
          </SurfaceCard>

          <SurfaceCard variant="subtle" padding="lg">
            <SectionHeader
              title={`Zoho Expense Ledger (${zohoExpenseProps.expenses.length} entries)`}
              description={`Organization ${zohoExpenseProps.orgId} • Total Verified: ${zohoExpenseProps.currency} ${zohoExpenseProps.totalAmount.toFixed(2)}`}
            />
            <Animated.View
              style={{
                opacity: listOpacity,
                transform: [{ translateY: listTranslateY }],
              }}
              className="gap-2.5"
            >
              {zohoExpenseProps.expenses.map((item) => (
                <View
                  key={item.expenseId}
                  className="p-3.5 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700 flex-row flex-wrap items-center justify-between gap-2"
                >
                  <View>
                    <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                      {item.merchant}
                    </Text>
                    <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-0.5">
                      {item.category} • {item.expenseId}
                    </Text>
                  </View>
                  <View className="flex-row items-center gap-2.5">
                    <Text className="text-sm font-bold text-canvas-900 dark:text-canvas-50">
                      {item.currency} {item.amount.toFixed(2)}
                    </Text>
                    <Badge
                      label={item.status}
                      tone={item.status === 'Approved' || item.status === 'Reimbursed' ? 'success' : 'info'}
                    />
                  </View>
                </View>
              ))}
            </Animated.View>
          </SurfaceCard>
        </View>
      ) : null}
    </View>
  );
}
