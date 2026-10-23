// Rules: R-01 Single bridge door via gmailApi, L-04 9-Phase Intent Machine (T2 requiresConfirm), N-06, N-07
import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { View, Text } from 'react-native';
import {
  SurfaceCard,
  SectionHeader,
  FormField,
  ActionButton,
  Badge,
  IntentStatusBanner,
} from '../../../ui';
import { useMutationIntent } from '../../../intent/useMutationIntent';
import { GmailApi, createGmailApi } from '../api/gmailApi';

export interface GmailWorkspaceScreenProps {
  readonly api?: GmailApi | undefined;
  readonly bridge?: Parameters<typeof createGmailApi>[0] | undefined;
  readonly canSend?: boolean | undefined;
  readonly onDirtyChange?: ((isDirty: boolean) => void) | undefined;
}

interface SentMessageItem {
  readonly id: string;
  readonly recipient: string;
  readonly subject: string;
  readonly sentAt: string;
}

export function GmailWorkspaceScreen({
  api,
  bridge,
  canSend = true,
  onDirtyChange,
}: GmailWorkspaceScreenProps) {
  const gmailApi = useMemo(() => {
    if (api) return api;
    if (bridge) return createGmailApi(bridge);
    throw new Error('GmailWorkspaceScreen requires either an api or bridge prop');
  }, [api, bridge]);

  const [recipientEmail, setRecipientEmail] = useState('');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');

  // Per-field validation error states
  const [recipientError, setRecipientError] = useState<string | null>(null);
  const [subjectError, setSubjectError] = useState<string | null>(null);
  const [bodyError, setBodyError] = useState<string | null>(null);

  const [sentMessages, setSentMessages] = useState<readonly SentMessageItem[]>([]);

  const hasDraft = Boolean(recipientEmail.trim() || subject.trim() || body.trim());

  useEffect(() => {
    onDirtyChange?.(hasDraft);
  }, [hasDraft, onDirtyChange]);

  const sendMutation = useCallback(
    async (
      payload: { recipientEmail: string; subject: string; body: string },
      correlationId: any,
      idempotencyKey: any
    ) => {
      const res = await gmailApi.sendMessage(
        payload.recipientEmail,
        payload.subject,
        payload.body,
        correlationId,
        idempotencyKey
      );
      if (res.ok) {
        setSentMessages((prev) => [
          {
            id: res.value?.messageId ?? `msg_${Date.now()}`,
            recipient: payload.recipientEmail,
            subject: payload.subject,
            sentAt: new Date().toISOString(),
          },
          ...prev,
        ]);
        // Clear draft on successful send
        setRecipientEmail('');
        setSubject('');
        setBody('');
      }
      return res;
    },
    [gmailApi]
  );

  const { state: sendIntent, execute: executeSend, confirm: confirmSend, reset: resetSend } =
    useMutationIntent(sendMutation, 'gmail.send');

  const handleReviewSend = () => {
    let isValid = true;

    // Validate recipient email
    if (!recipientEmail.trim() || !recipientEmail.includes('@') || !recipientEmail.includes('.')) {
      setRecipientError('Please enter a valid recipient email address (e.g. name@domain.com).');
      isValid = false;
    } else {
      setRecipientError(null);
    }

    // Validate subject
    if (!subject.trim()) {
      setSubjectError('Message subject is required.');
      isValid = false;
    } else {
      setSubjectError(null);
    }

    // Validate body
    if (!body.trim()) {
      setBodyError('Message body cannot be empty.');
      isValid = false;
    } else {
      setBodyError(null);
    }

    if (!isValid) return;

    // Tier T2 policy requires confirmation before sending external email
    executeSend(
      {
        recipientEmail: recipientEmail.trim(),
        subject: subject.trim(),
        body: body.trim(),
      },
      true
    );
  };

  const handleClearDraft = () => {
    setRecipientEmail('');
    setSubject('');
    setBody('');
    setRecipientError(null);
    setSubjectError(null);
    setBodyError(null);
    resetSend();
  };

  return (
    <View className="gap-6">
      <IntentStatusBanner
        state={sendIntent}
        operationSummary={`Send external email to "${recipientEmail}" with subject "${subject}"`}
        onConfirm={confirmSend}
        onReset={resetSend}
      />

      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title="Compose Operational Notification"
          description="Send an audited outbound message via Gmail (Tier T2). Draft content is preserved locally while reviewing."
          action={
            hasDraft ? (
              <Badge label="Draft preserved" tone="warning" />
            ) : (
              <Badge label="Ready" tone="info" />
            )
          }
        />

        <FormField
          label="Recipient Email"
          value={recipientEmail}
          onChangeText={(val) => {
            setRecipientEmail(val);
            if (recipientError) setRecipientError(null);
          }}
          keyboardType="email-address"
          placeholder="operator@organization.com"
          editable={canSend && sendIntent.phase !== 'Submitting'}
          required
          errorMessage={recipientError ?? undefined}
          onClear={() => {
            setRecipientEmail('');
            setRecipientError(null);
          }}
        />

        <FormField
          label="Subject"
          value={subject}
          onChangeText={(val) => {
            setSubject(val);
            if (subjectError) setSubjectError(null);
          }}
          placeholder="Operational status update"
          editable={canSend && sendIntent.phase !== 'Submitting'}
          required
          errorMessage={subjectError ?? undefined}
          onClear={() => {
            setSubject('');
            setSubjectError(null);
          }}
        />

        <FormField
          label="Message Body"
          value={body}
          onChangeText={(val) => {
            setBody(val);
            if (bodyError) setBodyError(null);
          }}
          placeholder="Write a concise operational message…"
          multiline
          editable={canSend && sendIntent.phase !== 'Submitting'}
          required
          errorMessage={bodyError ?? undefined}
          onClear={() => {
            setBody('');
            setBodyError(null);
          }}
        />

        {/* Pre-Send Review Note */}
        <View className="mb-4">
          <SurfaceCard variant="subtle" padding="sm">
            <Text className="text-xs text-status-warningText font-medium">
              Consequence: External emails cannot be recalled once sent. Tier T2 requires explicit confirmation before delivery.
            </Text>
          </SurfaceCard>
        </View>

        <View className="flex-row flex-wrap gap-3">
          <ActionButton
            label="Review & Send Message"
            onPress={handleReviewSend}
            disabled={!canSend}
            disabledReason={!canSend ? 'Missing gmail.messages.send permission' : undefined}
            isSubmitting={sendIntent.phase === 'Submitting'}
            submittingLabel="Sending message…"
            variant="primary"
          />
          {hasDraft && (
            <ActionButton
              label="Clear Draft"
              variant="secondary"
              onPress={handleClearDraft}
            />
          )}
        </View>
      </SurfaceCard>

      {/* Sent Message History */}
      {sentMessages.length > 0 && (
        <SurfaceCard variant="elevated" padding="lg">
          <SectionHeader
            title="Sent Messages"
            description="Operational emails dispatched during this session."
            action={<Badge label={`${sentMessages.length} sent`} tone="success" />}
          />
          <View className="gap-3">
            {sentMessages.map((msg) => (
              <SurfaceCard key={msg.id} variant="subtle" padding="sm">
                <View className="flex-row items-center justify-between">
                  <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                    {msg.subject}
                  </Text>
                  <Text className="text-xs text-canvas-500 dark:text-canvas-400">
                    {msg.sentAt.slice(11, 19)} UTC
                  </Text>
                </View>
                <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
                  Recipient: {msg.recipient} • ID: {msg.id}
                </Text>
              </SurfaceCard>
            ))}
          </View>
        </SurfaceCard>
      )}
    </View>
  );
}
