import { Permission } from '../../contracts/permissions';
import { RiskTier } from '../../contracts/riskTiers';

export interface FeatureRoute {
  name: string;
  requiredPermission?: Permission;
  tier: RiskTier;
}

export interface FeatureManifest {
  id: string;
  name: string;
  owner: string;
  maxTier: RiskTier;
  routes: FeatureRoute[];
  commands: string[];
}

export const templateManifest: FeatureManifest = {
  id: '_template',
  name: 'Template Feature',
  owner: '@architecture-team',
  maxTier: 'T0',
  routes: [
    {
      name: 'TemplateHome',
      tier: 'T0',
    },
  ],
  commands: [],
};
