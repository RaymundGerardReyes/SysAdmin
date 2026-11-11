// Rules: R-01 Single bridge door via biginApi, R-04 No cross-feature imports, L-04 9-Phase Intent Machine, N-06, N-07
import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { View, Text } from 'react-native';
import { IntegrationWorkspace } from '../../../ui';
import { useMutationIntent } from '../../../intent/useMutationIntent';
import { createCorrelationId } from '../../../core/ids/correlationId';
import {
  BiginContactItemContract,
  BiginModuleItemResponse,
  BiginNotificationDetailsContract,
  BiginOrgDetailsContract,
  CreateBiginContactRequest,
  CreateBiginContactResponse,
} from '../../../contracts/dtos';
import { BiginApi, createBiginApi } from '../api/biginApi';
import { biginManifest } from '../feature.manifest';

export interface BiginWorkspaceScreenProps {
  readonly api?: BiginApi | undefined;
  readonly bridge?: Parameters<typeof createBiginApi>[0] | undefined;
  readonly dataCenter?: string | undefined;
  readonly canRead?: boolean | undefined;
  readonly canCreate?: boolean | undefined;
  readonly isAuthenticated?: boolean | undefined;
  readonly onSignIn?: (() => void) | undefined;
  readonly onInspectJobs?: (() => void) | undefined;
  readonly onDirtyChange?: ((isDirty: boolean) => void) | undefined;
}

const DEFAULT_FULL_NAME = 'Sophia Reyes';
const DEFAULT_EMAIL = 's.reyes@partner-enterprise.io';
const DEFAULT_ACCOUNT_NAME = 'Partner Enterprise Holdings';
const DEFAULT_TITLE = 'Director of Settlements';

export function BiginWorkspaceScreen({
  api,
  bridge,
  dataCenter = 'US',
  canRead = true,
  canCreate = true,
  isAuthenticated = true,
  onSignIn = () => {},
  onInspectJobs,
  onDirtyChange,
}: BiginWorkspaceScreenProps) {
  const biginApi = useMemo(() => {
    if (api) return api;
    if (bridge) return createBiginApi(bridge);
    throw new Error('BiginWorkspaceScreen requires either an api or bridge prop');
  }, [api, bridge]);

  // Synchronized Zoho Bigin telemetry & records state
  const [org, setOrg] = useState<BiginOrgDetailsContract | undefined>(undefined);
  const [modules, setModules] = useState<readonly BiginModuleItemResponse[]>([]);
  const [watchChannels, setWatchChannels] = useState<readonly BiginNotificationDetailsContract[]>([]);
  const [contacts, setContacts] = useState<readonly BiginContactItemContract[]>([]);
  const [isSyncing, setIsSyncing] = useState(false);
  const [syncError, setSyncError] = useState<string | null>(null);

  // Create Contact form state
  const [fullName, setFullName] = useState(DEFAULT_FULL_NAME);
  const [email, setEmail] = useState(DEFAULT_EMAIL);
  const [accountName, setAccountName] = useState(DEFAULT_ACCOUNT_NAME);
  const [title, setTitle] = useState(DEFAULT_TITLE);
  const [validationError, setValidationError] = useState<string | null>(null);

  const isDirty =
    fullName !== DEFAULT_FULL_NAME ||
    email !== DEFAULT_EMAIL ||
    accountName !== DEFAULT_ACCOUNT_NAME ||
    title !== DEFAULT_TITLE;

  useEffect(() => {
    onDirtyChange?.(isDirty);
  }, [isDirty, onDirtyChange]);

  // 1. Read / Synchronize Contacts, Org Profile, Modules & Watch Channels (T0 Read)
  const handleSyncBigin = useCallback(async () => {
    if (!canRead) return;
    setIsSyncing(true);
    setSyncError(null);
    const correlationId = createCorrelationId();
    const res = await biginApi.readContacts({ dataCenter }, correlationId);
    setIsSyncing(false);

    if (res.ok) {
      setContacts(res.value.contacts);
      setOrg(res.value.org);
      setModules(res.value.modules);
      setWatchChannels(res.value.notifications.watch);
    } else {
      setSyncError(res.error.message);
    }
  }, [biginApi, canRead, dataCenter]);

  // Auto-synchronize on initial mount when authenticated and permitted
  useEffect(() => {
    if (isAuthenticated && canRead && contacts.length === 0) {
      void handleSyncBigin();
    }
  }, [isAuthenticated, canRead, contacts.length, handleSyncBigin]);

  // 2. Create Protected Bigin Contact (T2 Mutation with Confirmation & Idempotency)
  const createContactMutation = useCallback(
    async (
      payload: CreateBiginContactRequest,
      correlationId: any,
      idempotencyKey: any
    ) => {
      const res = await biginApi.createContact(payload, correlationId, idempotencyKey);
      if (res.ok) {
        setContacts((prev) => [res.value.contact, ...prev]);
        // Reset form to clean state after successful creation
        setFullName(DEFAULT_FULL_NAME);
        setEmail(DEFAULT_EMAIL);
        setAccountName(DEFAULT_ACCOUNT_NAME);
        setTitle(DEFAULT_TITLE);
      }
      return res;
    },
    [biginApi]
  );

  const {
    state: createIntent,
    execute: executeCreate,
    confirm: confirmCreate,
    reset: resetCreate,
  } = useMutationIntent<CreateBiginContactRequest, CreateBiginContactResponse>(
    createContactMutation,
    'bigin.contacts.create'
  );

  const handlePrepareCreateContact = useCallback(() => {
    if (!fullName.trim()) {
      setValidationError('Contact full name is required.');
      return;
    }
    if (!email.trim() || !email.includes('@') || !email.includes('.')) {
      setValidationError('A valid work email address is required.');
      return;
    }
    if (!accountName.trim()) {
      setValidationError('Account / Company name is required.');
      return;
    }

    setValidationError(null);
    // Tier T2 policy: requiresConfirm = true
    void executeCreate(
      {
        fullName: fullName.trim(),
        email: email.trim(),
        accountName: accountName.trim(),
        title: title.trim() || 'Operations Contact',
        dataCenter,
      },
      true
    );
  }, [accountName, dataCenter, email, executeCreate, fullName, title]);

  const routes = useMemo(() => biginManifest.routes.map((r) => r.name), []);

  return (
    <View className="gap-4">
      {syncError ? (
        <Text
          accessibilityRole="alert"
          className="text-xs font-medium text-status-dangerText px-1"
        >
          {syncError}
        </Text>
      ) : null}

      {validationError ? (
        <Text
          accessibilityRole="alert"
          className="text-xs font-medium text-status-dangerText px-1"
        >
          {validationError}
        </Text>
      ) : null}

      <IntegrationWorkspace
        name={biginManifest.name}
        description="Review protected contacts, organization profile, and webhook watches through Zoho Bigin."
        routes={routes}
        canAccess={isAuthenticated && canRead}
        onSignIn={onSignIn}
        onInspectJobs={onInspectJobs}
        biginProps={{
          org,
          modules,
          watchChannels,
          contacts,
          isSyncing,
          canRead,
          canCreate,
          fullName,
          email,
          accountName,
          title,
          onChangeFullName: (val) => {
            setFullName(val);
            if (validationError) setValidationError(null);
          },
          onChangeEmail: (val) => {
            setEmail(val);
            if (validationError) setValidationError(null);
          },
          onChangeAccountName: (val) => {
            setAccountName(val);
            if (validationError) setValidationError(null);
          },
          onChangeTitle: setTitle,
          onSync: () => {
            void handleSyncBigin();
          },
          onCreateContact: handlePrepareCreateContact,
          intentState: createIntent,
          onConfirmIntent: () => {
            void confirmCreate();
          },
          onCancelIntent: resetCreate,
          onResetIntent: resetCreate,
        }}
      />
    </View>
  );
}
