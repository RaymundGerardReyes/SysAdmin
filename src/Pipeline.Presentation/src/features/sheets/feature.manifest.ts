import { FeatureManifest } from '../_template/feature.manifest';

export const sheetsManifest: FeatureManifest = {
  id: 'sheets',
  name: 'Google Sheets Integration',
  owner: '@sheets-team',
  maxTier: 'T1',
  routes: [
    {
      name: 'SheetsExplorer',
      requiredPermission: 'sheets.values.read',
      tier: 'T0',
    },
    {
      name: 'SheetsEditor',
      requiredPermission: 'sheets.values.write',
      tier: 'T1',
    },
  ],
  commands: ['sheets.read', 'sheets.write'],
};
