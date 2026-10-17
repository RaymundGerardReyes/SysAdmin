// Rules: R-01 Single bridge door via gmailApi, L-04 9-Phase Intent Machine (T2 requiresConfirm), N-06, N-07
import React, { useState, useCallback } from 'react';
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
import { GmailApi } from '../api/gmailApi';

export interface GmailWorkspaceScreenProps {
  readonly api: GmailApi;
  readonly canSend: boolean;
}

export function GmailWorkspaceScreen({ api: gmailApi, canSend }: GmailWorkspaceScreenProps) {
  const [recipientEmail, setRecipientEmail] = useState('');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const hasDraft = Boolean(recipientEmail.trim() || subject.trim() || body.trim());

  const sendMutation = useCallback(
    async (
      payload: { recipientEmail: string; subject: string; body: string },
      correlationId: any,
      idempotencyKey: any
    ) => {
      return gmailApi.sendMessage(
        payload.recipientEmail,
        payload.subject,
        payload.body,
        correlationId,
        idempotencyKey
      );
    },
    [gmailApi]
  );

  const { state: sendIntent, execute: executeSend, confirm: confirmSend, reset: resetSend } =
    useMutationIntent(sendMutation, 'gmail.send');

  const handleReviewSend = () => {
    if (!recipientEmail.includes('@')) {
      setFormError('Please enter a valid recipient email address.');
      return;
    }
    if (!subject.trim()) {
      setFormError('Please enter a message subject.');
      return;
    }
    if (!body.trim()) {
      setFormError('Message body cannot be empty.');
      return;
    }
    setFormError(null);
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

  return (
    <View className="space-y-6">
      <IntentStatusBanner
        state={sendIntent}
        operationSummary={`Send email to "${recipientEmail}" with subject "${subject}"`}
        onConfirm={confirmSend}
        onReset={resetSend}
      />

      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title="Compose Operational Notification"
          description="Send an audited outbound message via Gmail (Tier T2). Draft content is preserved locally while reviewing."
          action={
            hasDraft ? (
              <Badge label="Draft preserved" tone="neutral" />
            ) : (
              <Badge label="Ready" tone="info" />
            )
          }
        />

        <FormField
          label="Recipient Email"
          value={recipientEmail}
          onChangeText={setRecipientEmail}
          keyboardType="email-address"
          placeholder="operator@organization.com"
          editable={canSend && sendIntent.phase !== 'Submitting'}
        />

        <FormField
          label="Subject"
          value={subject}
          onChangeText={setSubject}
          placeholder="Operational status update"
          editable={canSend && sendIntent.phase !== 'Submitting'}
        />

        <FormField
          label="Message Body"
          value={body}
          onChangeText={setBody}
          placeholder="Write a concise operational message…"
          multiline
          editable={canSend && sendIntent.phase !== 'Submitting'}
        />

        {formError ? (
          <Text className="text-xs font-medium text-status-dangerText mb-3">
            × {formError}
          </Text>
        ) : null}

        <View className="flex-row flex-wrap gap-3">
          <ActionButton
            label="Review & Send Message"
            onPress={handleReviewSend}
            disabled={!canSend}
            isSubmitting={sendIntent.phase === 'Submitting'}
            submittingLabel="Sending message…"
            variant="primary"
          />
          {hasDraft && (
            <ActionButton
              label="Clear Draft"
              variant="secondary"
              onPress={() => {
                setRecipientEmail('');
                setSubject('');
                setBody('');
                setFormError(null);
                resetSend();
              }}
            />
          )}
        </View>
      </SurfaceCard>
    </View>
  );
}

