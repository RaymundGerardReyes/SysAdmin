import { FeatureManifest } from '../_template/feature.manifest';

export const sessionManifest: FeatureManifest = {
  id: 'session',
  name: 'User Session & Identity',
  owner: '@security-team',
  maxTier: 'T0',
  routes: [{ name: 'Login', tier: 'T0' }],
  commands: [],
};
