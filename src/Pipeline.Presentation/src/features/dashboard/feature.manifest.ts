import { FeatureManifest } from '../_template/feature.manifest';

export const dashboardManifest: FeatureManifest = {
  id: 'dashboard',
  name: 'Operations Dashboard',
  owner: '@dashboard-team',
  maxTier: 'T0',
  routes: [{ name: 'DashboardHome', tier: 'T0' }],
  commands: [],
};
