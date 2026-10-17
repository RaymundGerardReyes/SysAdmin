import { FeatureManifest } from '../_template/feature.manifest';

export const gmailManifest: FeatureManifest = {
  id: 'gmail',
  name: 'Gmail Integration',
  owner: '@gmail-team',
  maxTier: 'T2',
  routes: [
    {
      name: 'GmailComposer',
      requiredPermission: 'gmail.messages.send',
      tier: 'T2',
    },
  ],
  commands: ['gmail.send'],
};
