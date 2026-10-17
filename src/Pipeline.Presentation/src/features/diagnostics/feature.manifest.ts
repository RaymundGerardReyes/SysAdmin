import { FeatureManifest } from '../_template/feature.manifest';

export const diagnosticsManifest: FeatureManifest = {
  id: 'diagnostics',
  name: 'System Diagnostics & Support',
  owner: '@support-team',
  maxTier: 'T0',
  routes: [
    {
      name: 'SupportBundle',
      tier: 'T0',
    },
  ],
  commands: [],
};
