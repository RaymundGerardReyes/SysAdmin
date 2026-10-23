import { FeatureManifest } from '../_template/feature.manifest';

export const chatManifest: FeatureManifest = {
  id: 'chat',
  name: 'Google Chat',
  owner: '@google-chat-team',
  maxTier: 'T2',
  routes: [
    {
      name: 'ChatSpaces',
      requiredPermission: 'chat.messages.create',
      tier: 'T2',
    },
  ],
  commands: ['chat.create'],
};
