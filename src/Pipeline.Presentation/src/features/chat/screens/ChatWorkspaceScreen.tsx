// Rules: R-01 Single bridge door via chatApi, R-04 No cross-feature imports, L-04 9-Phase Intent Machine, N-06, N-07
import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { IntegrationWorkspace } from '../../../ui';
import { useMutationIntent } from '../../../intent/useMutationIntent';
import {
  ChatMessageContract,
  CreateChatMessageRequest,
  CreateChatMessageResponse,
} from '../../../contracts/dtos';
import { ChatApi, createChatApi } from '../api/chatApi';
import { chatManifest } from '../feature.manifest';

export interface ChatWorkspaceScreenProps {
  readonly api?: ChatApi | undefined;
  readonly bridge?: Parameters<typeof createChatApi>[0] | undefined;
  readonly canPost?: boolean | undefined;
  readonly isAuthenticated?: boolean | undefined;
  readonly onSignIn?: (() => void) | undefined;
  readonly onInspectJobs?: (() => void) | undefined;
  readonly onDirtyChange?: ((isDirty: boolean) => void) | undefined;
}

const DEFAULT_SPACE_ID = 'spaces/ops-alerts';
const DEFAULT_THREAD_KEY = 'ops-incident-bridge';
const DEFAULT_MESSAGE_TEXT =
  'All Q4 settlement connectors verified healthy under capability ticket enforcement.';

const INITIAL_MESSAGES: readonly ChatMessageContract[] = [
  {
    messageId: 'spaces/ops-alerts/messages/msg_401',
    spaceId: 'spaces/ops-alerts',
    sender: 'release-bot@pipeline.local',
    text: '[AUTHZ_P01..P11] Daily capability ticket verification completed across 6 connectors.',
    threadKey: 'ops-daily-verification',
    createdAtUtc: '2026-10-23T09:00:00Z',
  },
  {
    messageId: 'spaces/ops-alerts/messages/msg_402',
    spaceId: 'spaces/ops-alerts',
    sender: 'treasury-watch@pipeline.local',
    text: 'PayMongo settlement batch #2026-10-23 reconciled with 0 discrepancies.',
    threadKey: 'settlement-reconcile',
    createdAtUtc: '2026-10-23T10:30:00Z',
  },
];

export function ChatWorkspaceScreen({
  api,
  bridge,
  canPost = true,
  isAuthenticated = true,
  onSignIn = () => {},
  onInspectJobs,
  onDirtyChange,
}: ChatWorkspaceScreenProps) {
  const chatApi = useMemo(() => {
    if (api) return api;
    if (bridge) return createChatApi(bridge);
    throw new Error('ChatWorkspaceScreen requires either an api or bridge prop');
  }, [api, bridge]);

  const [spaceId, setSpaceId] = useState(DEFAULT_SPACE_ID);
  const [threadKey, setThreadKey] = useState(DEFAULT_THREAD_KEY);
  const [messageText, setMessageText] = useState(DEFAULT_MESSAGE_TEXT);
  const [messages, setMessages] = useState<readonly ChatMessageContract[]>(INITIAL_MESSAGES);

  const isDirty =
    spaceId !== DEFAULT_SPACE_ID ||
    threadKey !== DEFAULT_THREAD_KEY ||
    messageText !== DEFAULT_MESSAGE_TEXT;

  useEffect(() => {
    onDirtyChange?.(isDirty);
  }, [isDirty, onDirtyChange]);

  const chatMutation = useMutationIntent<CreateChatMessageRequest, CreateChatMessageResponse>(
    useCallback(
      async (payload, correlationId, idempotencyKey) => {
        const res = await chatApi.postMessage(payload, correlationId, idempotencyKey);
        if (res.ok) {
          setMessages(res.value.history);
        }
        return res;
      },
      [chatApi]
    ),
    'chat.create'
  );

  const routes = useMemo(() => chatManifest.routes.map((r) => r.name), []);

  return (
    <IntegrationWorkspace
      name={chatManifest.name}
      description="Review protected spaces and broadcast operational alerts through Google Chat."
      routes={routes}
      canAccess={isAuthenticated && canPost}
      onSignIn={onSignIn}
      onInspectJobs={onInspectJobs}
      chatProps={{
        spaceId,
        threadKey,
        messageText,
        messages,
        canPost,
        onChangeSpaceId: setSpaceId,
        onChangeThreadKey: setThreadKey,
        onChangeMessageText: setMessageText,
        onSendMessage: () => {
          void chatMutation.execute(
            {
              spaceId: spaceId.trim(),
              text: messageText.trim(),
              threadKey: threadKey.trim(),
            },
            true
          );
        },
        intentState: chatMutation.state,
        onConfirmIntent: () => {
          void chatMutation.confirm();
        },
        onCancelIntent: chatMutation.reset,
        onResetIntent: chatMutation.reset,
      }}
    />
  );
}
