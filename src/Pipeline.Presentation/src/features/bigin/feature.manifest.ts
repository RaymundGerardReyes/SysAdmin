import { FeatureManifest } from '../_template/feature.manifest';

export const biginManifest: FeatureManifest = {
  id: 'bigin',
  name: 'Zoho Bigin',
  owner: '@bigin-team',
  maxTier: 'T1',
  routes: [
    {
      name: 'BiginContacts',
      requiredPermission: 'bigin.contacts.read',
      tier: 'T0',
    },
    {
      name: 'BiginContactCreate',
      requiredPermission: 'bigin.contacts.create',
      tier: 'T1',
    },
  ],
  commands: ['bigin.contacts.read', 'bigin.contacts.create'],
};
