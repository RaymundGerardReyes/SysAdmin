import { FeatureManifest } from '../_template/feature.manifest';

export const jobsManifest: FeatureManifest = {
  id: 'jobs',
  name: 'Job History & Status',
  owner: '@jobs-team',
  maxTier: 'T1',
  routes: [
    {
      name: 'JobList',
      requiredPermission: 'jobs.read',
      tier: 'T0',
    },
    {
      name: 'JobDetail',
      requiredPermission: 'jobs.read',
      tier: 'T0',
    },
  ],
  commands: ['jobs.read', 'jobs.cancel'],
};
