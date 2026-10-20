// Rules: L-01 Layer 7 Composition Root, R-03 Cosmetic capability gating, R-06 Five screen states, N-06, N-07
import React, { useState, useMemo, useCallback, useEffect, useRef } from 'react';
import { View, Text } from 'react-native';
import { FEATURE_REGISTRY } from './featureRegistry';
import { PlatformShell, ShellNavItem } from '../platform/PlatformShell';
import {
  SurfaceCard,
  SectionHeader,
  Badge,
  ActionButton,
  EmptyState,
  ScreenStateView,
  ScreenLifecycleState,
  IntegrationWorkspace,
  useAppTheme,
} from '../ui';
import { useUiStore } from '../state/stores/uiStore';
import { useSessionStore } from '../state/stores/sessionStore';
import { usePendingOperationsStore } from '../state/stores/pendingOperationsStore';
import { resetAllStores } from '../state/stores/resetAllStores';
import { PERMISSIONS } from '../contracts/permissions';
import { breadcrumbs } from '../observability/breadcrumbs';
import { generateSupportBundle } from '../observability/supportBundle';
import { track } from '../observability/track';
import { createPipelineBridge } from '../bridge/createPipelineBridge';
import { createSheetsApi } from '../features/sheets/api/sheetsApi';
import { createPaymentsApi } from '../features/payments/api/paymentsApi';
import { createGmailApi } from '../features/gmail/api/gmailApi';
import { createJobsApi } from '../features/jobs/api/jobsApi';
import { SheetsWorkspaceScreen } from '../features/sheets/screens/SheetsWorkspaceScreen';
import { PaymentsWorkspaceScreen } from '../features/payments/screens/PaymentsWorkspaceScreen';
import { GmailWorkspaceScreen } from '../features/gmail/screens/GmailWorkspaceScreen';
import { JobsWorkspaceScreen } from '../features/jobs/screens/JobsWorkspaceScreen';

const defaultBridge = createPipelineBridge();

const FEATURE_SYMBOLS: Record<string, string> = {
  dashboard: '⊞',
  sheets: '▤',
  gmail: '✉',
  payments: '◈',
  jobs: '⟳',
  'zoho-expense': '⌁',
  bigin: '◌',
  chat: '◉',
  session: '⛨',
  diagnostics: '⚙',
};

export function AppBootstrap() {
  const { isCompact, isExpanded, setColorScheme } = useAppTheme();
  const { sidebarOpen, toggleSidebar, themeMode, setThemeMode } = useUiStore();
  const { isAuthenticated, userId, capabilities, setSession } = useSessionStore();
  const pendingOperations = usePendingOperationsStore((s) => s.operations);

  const setColorSchemeRef = useRef(setColorScheme);
  useEffect(() => {
    setColorSchemeRef.current = setColorScheme;
  }, [setColorScheme]);

  useEffect(() => {
    setColorSchemeRef.current?.(themeMode);
  }, [themeMode]);

  const [activeFeatureId, setActiveFeatureId] = useState<string>('dashboard');
  const [operatorInputId] = useState<string>('operator@pipeline.local');
  const [supportBundleJson, setSupportBundleJson] = useState<string | null>(null);

  // Unsaved Changes Navigation Guard state
  const [hasUnsavedWork, setHasUnsavedWork] = useState(false);
  const [pendingNavDestination, setPendingNavDestination] = useState<string | null>(null);

  const sheetsApi = useMemo(() => createSheetsApi(defaultBridge), []);
  const paymentsApi = useMemo(() => createPaymentsApi(defaultBridge), []);
  const gmailApi = useMemo(() => createGmailApi(defaultBridge), []);
  const jobsApi = useMemo(() => createJobsApi(defaultBridge), []);

  const handleNavigate = useCallback(
    (destId: string) => {
      if (destId === activeFeatureId) return;
      if (hasUnsavedWork) {
        setPendingNavDestination(destId);
        return;
      }
      setActiveFeatureId(destId);
    },
    [activeFeatureId, hasUnsavedWork]
  );

  const handleConfirmDiscardAndNavigate = useCallback(() => {
    if (pendingNavDestination) {
      setHasUnsavedWork(false);
      setActiveFeatureId(pendingNavDestination);
      setPendingNavDestination(null);
    }
  }, [pendingNavDestination]);

  const handleCancelNavigation = useCallback(() => {
    setPendingNavDestination(null);
  }, []);

  const establishDefaultSession = useCallback(() => {
    setSession(operatorInputId.trim() || 'operator@pipeline.local', {
      userId: operatorInputId.trim() || 'operator@pipeline.local',
      roles: ['OperationsAdministrator'],
      permissions: [...PERMISSIONS],
      scopes: {
        spreadsheetIds: ['ops-ledger-2026'],
        chatSpaceIds: [],
        zohoOrgIds: [],
        paymentAccountIds: ['acct_paymongo_primary'],
      },
      stepUpFresh: true,
    });
    track('session.established', { userId: operatorInputId.trim() || 'operator@pipeline.local' });
  }, [operatorInputId, setSession]);

  const establishRestrictedSession = useCallback(() => {
    setSession('viewer@pipeline.local', {
      userId: 'viewer@pipeline.local',
      roles: ['ReadOnlyAuditor'],
      permissions: ['jobs.read', 'sheets.values.read'],
      scopes: {
        spreadsheetIds: ['ops-ledger-2026'],
        chatSpaceIds: [],
        zohoOrgIds: [],
        paymentAccountIds: [],
      },
      stepUpFresh: false,
    });
    track('session.established', { userId: 'viewer@pipeline.local' });
  }, [setSession]);

  const activeFeature = useMemo(
    () => FEATURE_REGISTRY.find((f) => f.id === activeFeatureId) ?? FEATURE_REGISTRY[1],
    [activeFeatureId]
  );

  const breadcrumbTrail: readonly string[] = useMemo(() => {
    return [
      'Workspace',
      activeFeature.id === 'dashboard' ? 'Overview' : activeFeature.name,
    ];
  }, [activeFeature]);

  // Derive real ScreenLifecycleState (Rule R-06) from actual session and capability state
  const derivedScreenState: ScreenLifecycleState = useMemo(() => {
    if (
      activeFeature.id === 'dashboard' ||
      activeFeature.id === 'session' ||
      activeFeature.id === 'diagnostics'
    ) {
      return 'success';
    }
    if (!isAuthenticated || !capabilities) {
      return 'needs-reauth';
    }
    const primaryPermission = activeFeature.routes[0]?.requiredPermission;
    if (primaryPermission && !capabilities.permissions.includes(primaryPermission)) {
      return 'access-denied';
    }
    return 'success';
  }, [activeFeature, capabilities, isAuthenticated]);

  const navItems: readonly ShellNavItem[] = useMemo(() => {
    const orderedIds = [
      'dashboard',
      'sheets',
      'gmail',
      'chat',
      'payments',
      'zoho-expense',
      'bigin',
      'jobs',
      'session',
      'diagnostics',
    ];
    return orderedIds
      .map((id) => FEATURE_REGISTRY.find((f) => f.id === id))
      .filter((f): f is NonNullable<typeof f> => Boolean(f))
      .map((f) => ({
        id: f.id,
        label: f.id === 'dashboard' ? 'Overview' : f.name,
        symbol: FEATURE_SYMBOLS[f.id] ?? '•',
        ...(f.id === 'jobs' && pendingOperations.size > 0
          ? { badgeCount: pendingOperations.size }
          : {}),
      }));
  }, [pendingOperations.size]);

  const recentEvents = breadcrumbs.getAll();

  const hasPermission = (perm: string) =>
    Boolean(capabilities?.permissions.includes(perm as any));

  return (
    <PlatformShell
      title={activeFeature.id === 'dashboard' ? 'Operations Overview' : activeFeature.name}
      subtitle={
        isAuthenticated && userId
          ? `Signed in as ${userId}`
          : 'Unauthenticated session — sign in to execute protected operations'
      }
      navItems={navItems}
      activeNavId={activeFeatureId}
      onSelectNav={handleNavigate}
      sidebarOpen={sidebarOpen}
      onToggleSidebar={toggleSidebar}
      sessionStatusLabel={isAuthenticated ? 'Authenticated' : 'Not signed in'}
      sessionAuthenticated={isAuthenticated}
      themeMode={themeMode}
      onSelectThemeMode={setThemeMode}
      breadcrumbTrail={breadcrumbTrail}
      canNavigateBack={activeFeatureId !== 'dashboard'}
      onNavigateBack={() => handleNavigate('dashboard')}
      headerActions={
        !isAuthenticated ? (
          <ActionButton
            label="Sign In"
            onPress={establishDefaultSession}
            variant="primary"
          />
        ) : (
          <ActionButton
            label="Sign Out"
            onPress={() => {
              resetAllStores();
              track('session.cleared');
            }}
            variant="secondary"
          />
        )
      }
    >
      {/* Minimalistic Workspace Identity Header */}
      <View className="mb-6 flex-row flex-wrap items-baseline justify-between gap-2 pb-4 border-b border-canvas-200 dark:border-canvas-800">
        <View>
          <Text className="text-xl font-semibold tracking-tight text-canvas-900 dark:text-canvas-50">
            Pipeline Presentation
          </Text>
          <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-0.5">
            Registered Features: {FEATURE_REGISTRY.map((f) => f.id).join(', ')}
          </Text>
        </View>
        <Badge
          label={isAuthenticated ? `Role: ${capabilities?.roles[0] ?? 'Operator'}` : 'Preview Mode'}
          tone={isAuthenticated ? 'accent' : 'neutral'}
          showSymbol={false}
        />
      </View>

      {/* Unsaved Changes Navigation Guard Warning Banner */}
      {pendingNavDestination && (
        <View className="mb-6">
          <SurfaceCard variant="accent" padding="md">
            <View className="flex-row items-center justify-between mb-2">
              <Badge label="Unsaved Changes Warning" tone="warning" />
            </View>
            <Text className="text-base font-semibold text-canvas-900 dark:text-canvas-50 mb-1">
              You have unsaved changes in this workspace
            </Text>
            <Text className="text-xs text-canvas-600 dark:text-canvas-300 mb-3">
              Switching workspaces will discard all uncommitted edits. Would you like to discard your edits and continue to {pendingNavDestination}, or stay on the current screen?
            </Text>
            <View className="flex-row flex-wrap gap-3">
              <ActionButton
                label="Discard Edits & Switch"
                onPress={handleConfirmDiscardAndNavigate}
                variant="destructive"
              />
              <ActionButton
                label="Stay Here & Keep Editing"
                onPress={handleCancelNavigation}
                variant="secondary"
              />
            </View>
          </SurfaceCard>
        </View>
      )}

      <ScreenStateView
        state={derivedScreenState}
        title={activeFeature.name}
        requiredPermission={activeFeature.routes[0]?.requiredPermission}
        onReauthenticate={establishDefaultSession}
      >
        {/* 1. OVERVIEW DASHBOARD */}
        {activeFeature.id === 'dashboard' && (
          <View className="gap-6">
            {/* Session & Operational Readiness Summary */}
            <SurfaceCard variant="elevated" padding="lg">
              <SectionHeader
                title="Workspace Readiness"
                description={
                  isAuthenticated
                    ? `Active session for ${userId} with ${capabilities?.permissions.length ?? 0} granted permission(s).`
                    : 'Your workspace is currently in read-only preview. Sign in to enable Google Sheets, Gmail, Payments, and Job operations.'
                }
                action={
                  <Badge
                    label={isAuthenticated ? 'Ready' : 'Authentication required'}
                    tone={isAuthenticated ? 'success' : 'warning'}
                  />
                }
              />
              {!isAuthenticated && (
                <View className="flex-row flex-wrap gap-3 mt-2">
                  <ActionButton
                    label="Sign In as Operations Admin"
                    onPress={establishDefaultSession}
                    variant="primary"
                  />
                  <ActionButton
                    label="Sign In as Read-Only Auditor"
                    onPress={establishRestrictedSession}
                    variant="secondary"
                  />
                </View>
              )}
            </SurfaceCard>

            {/* Connected Workspaces Responsive Grid */}
            <View>
              <SectionHeader
                title="Connected Workspaces"
                description="Select a tool to manage data, communications, payments, or background jobs."
              />
              <View className="flex-row flex-wrap -mx-2">
                {FEATURE_REGISTRY.filter((f) => f.id !== 'dashboard').map((feature) => {
                  const primaryPerm = feature.routes[0]?.requiredPermission;
                  const isPermitted = !primaryPerm || hasPermission(primaryPerm);
                  return (
                    <View
                      key={feature.id}
                      className={
                        isCompact
                          ? 'w-full px-2 mb-4'
                          : isExpanded
                          ? 'w-1/3 px-2 mb-4'
                          : 'w-1/2 px-2 mb-4'
                      }
                    >
                      <SurfaceCard variant="elevated" padding="md">
                        <View className="flex-row items-center justify-between gap-2 mb-2">
                          <Text
                            numberOfLines={1}
                            className="text-base font-semibold text-canvas-900 dark:text-canvas-50 flex-1"
                          >
                            {FEATURE_SYMBOLS[feature.id] ?? '•'}  {feature.name}
                          </Text>
                          <Badge
                            label={
                              !isAuthenticated && primaryPerm
                                ? 'Sign-in needed'
                                : isPermitted
                                ? 'Available'
                                : 'Restricted'
                            }
                            tone={
                              !isAuthenticated && primaryPerm
                                ? 'neutral'
                                : isPermitted
                                ? 'success'
                                : 'warning'
                            }
                          />
                        </View>
                        <Text
                          numberOfLines={1}
                          className="text-xs text-canvas-500 dark:text-canvas-400 mb-4"
                        >
                          {feature.routes.map((r) => r.name).join(' • ')}
                        </Text>
                        <ActionButton
                          label="Open Workspace"
                          variant="secondary"
                          onPress={() => handleNavigate(feature.id)}
                        />
                      </SurfaceCard>
                    </View>
                  );
                })}
              </View>
            </View>

            {/* Real Recent Activity Summary */}
            <SurfaceCard variant="elevated" padding="lg">
              <SectionHeader
                title="Recent Session Activity"
                description="Actual operations and events recorded during this session."
                action={
                  <ActionButton
                    label="View Job History"
                    variant="tertiary"
                    onPress={() => handleNavigate('jobs')}
                  />
                }
              />
              {recentEvents.length === 0 ? (
                <EmptyState
                  title="No recent activity in this session"
                  description="Operations you run across Sheets, Gmail, Payments, or Jobs will appear here with their verification status."
                />
              ) : (
                <View className="gap-1">
                  {recentEvents.slice(-5).reverse().map((ev, i) => (
                    <View
                      key={`${ev.timestamp}-${i}`}
                      className="py-2.5 border-b border-canvas-200 dark:border-canvas-800 flex-row items-center justify-between"
                    >
                      <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
                        {ev.event}
                      </Text>
                      <Text className="text-xs font-mono text-canvas-500 dark:text-canvas-400">
                        {ev.timestamp.slice(11, 19)} UTC
                      </Text>
                    </View>
                  ))}
                </View>
              )}
            </SurfaceCard>
          </View>
        )}

        {/* 2. GOOGLE SHEETS WORKSPACE */}
        {activeFeature.id === 'sheets' && (
          <SheetsWorkspaceScreen
            api={sheetsApi}
            canWrite={hasPermission('sheets.values.write')}
            onDirtyChange={setHasUnsavedWork}
          />
        )}

        {/* 3. GMAIL WORKSPACE */}
        {activeFeature.id === 'gmail' && (
          <GmailWorkspaceScreen
            api={gmailApi}
            canSend={hasPermission('gmail.messages.send')}
            onDirtyChange={setHasUnsavedWork}
          />
        )}

        {/* 4. PAYMENTS WORKSPACE */}
        {activeFeature.id === 'payments' && (
          <PaymentsWorkspaceScreen
            api={paymentsApi}
            canCreateCheckout={hasPermission('payments.checkout.create')}
            canApproveCheckout={hasPermission('payments.checkout.approve')}
            onInspectJobs={() => handleNavigate('jobs')}
            onDirtyChange={setHasUnsavedWork}
          />
        )}

        {/* 5. JOBS WORKSPACE */}
        {activeFeature.id === 'jobs' && <JobsWorkspaceScreen api={jobsApi} />}

        {/* Zoho Expense, Bigin, and Chat Integration Workspaces */}
        {(['zoho-expense', 'bigin', 'chat'] as const).includes(activeFeature.id as 'zoho-expense' | 'bigin' | 'chat') && (
          <IntegrationWorkspace
            name={activeFeature.name}
            description={
              activeFeature.id === 'zoho-expense'
                ? 'Review and manage protected expense workflows through Zoho Expense.'
                : activeFeature.id === 'bigin'
                ? 'Review protected contacts and relationship workflows through Zoho Bigin.'
                : 'Review protected spaces and messaging workflows through Google Chat.'
            }
            routes={activeFeature.routes.map((route) => route.name)}
            canAccess={Boolean(activeFeature.routes[0]?.requiredPermission && hasPermission(activeFeature.routes[0].requiredPermission))}
            onSignIn={establishDefaultSession}
          />
        )}

        {/* 6. SESSION & IDENTITY WORKSPACE */}
        {activeFeature.id === 'session' && (
          <SurfaceCard variant="elevated" padding="lg">
            <SectionHeader
              title="User Session & Identity"
              description="Manage your active operator session and inspect granted permissions."
            />
            {isAuthenticated && capabilities ? (
              <View className="gap-4">
                <View className="p-4 rounded-lg bg-canvas-100 dark:bg-canvas-800">
                  <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                    Signed in as: {userId}
                  </Text>
                  <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
                    Assigned Roles: {capabilities.roles.join(', ')} • Step-Up Fresh:{' '}
                    {capabilities.stepUpFresh ? 'Yes' : 'No'}
                  </Text>
                </View>
                <View>
                  <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400 mb-2">
                    Granted Permissions ({capabilities.permissions.length})
                  </Text>
                  <View className="flex-row flex-wrap gap-2">
                    {capabilities.permissions.map((perm) => (
                      <Badge key={perm} label={perm} tone="neutral" showSymbol={false} />
                    ))}
                  </View>
                </View>
                <ActionButton
                  label="Clear Session & Sign Out"
                  variant="destructive"
                  onPress={() => {
                    resetAllStores();
                    track('session.cleared');
                  }}
                />
              </View>
            ) : (
              <View className="gap-4">
                <Text className="text-sm text-canvas-500 dark:text-canvas-400">
                  Select a session profile to authenticate with the workspace:
                </Text>
                <View className="flex-row flex-wrap gap-3">
                  <ActionButton
                    label="Sign In as Operations Admin (Full Access)"
                    onPress={establishDefaultSession}
                    variant="primary"
                  />
                  <ActionButton
                    label="Sign In as Read-Only Auditor (Restricted)"
                    onPress={establishRestrictedSession}
                    variant="secondary"
                  />
                </View>
              </View>
            )}
          </SurfaceCard>
        )}

        {/* 7. SYSTEM DIAGNOSTICS & SUPPORT WORKSPACE */}
        {activeFeature.id === 'diagnostics' && (
          <SurfaceCard variant="elevated" padding="lg">
            <SectionHeader
              title="System Diagnostics & Support Bundle"
              description="Export redacted session breadcrumbs and build metadata for support troubleshooting."
            />
            <ActionButton
              label="Generate Support Bundle"
              variant="secondary"
              onPress={() => {
                const bundle = generateSupportBundle();
                setSupportBundleJson(JSON.stringify(bundle, null, 2));
              }}
            />
            {supportBundleJson ? (
              <View className="mt-4 p-4 rounded-lg bg-canvas-100 dark:bg-canvas-800 border border-canvas-200 dark:border-canvas-700">
                <Text className="text-xs font-mono text-canvas-900 dark:text-canvas-50">
                  {supportBundleJson}
                </Text>
              </View>
            ) : null}
          </SurfaceCard>
        )}
      </ScreenStateView>
    </PlatformShell>
  );
}
