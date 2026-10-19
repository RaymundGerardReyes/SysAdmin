import { FeatureManifest } from '../_template/feature.manifest';

export const chatManifest: FeatureManifest = {
  id: 'chat',
  name: 'Google Chat',
  owner: '@google-chat-team',
  maxTier: 'T1',
  routes: [
    {
      name: 'ChatSpaces',
      requiredPermission: 'chat.messages.create',
      tier: 'T0',
    },
  ],
  commands: ['chat.messages.create'],
};
