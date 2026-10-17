import { FeatureManifest } from '../_template/feature.manifest';

export const paymentsManifest: FeatureManifest = {
  id: 'payments',
  name: 'PayMongo Payments',
  owner: '@payments-team',
  maxTier: 'T3',
  routes: [
    {
      name: 'PaymentsStatus',
      requiredPermission: 'payments.status.read',
      tier: 'T0',
    },
    {
      name: 'CreateCheckout',
      requiredPermission: 'payments.checkout.create',
      tier: 'T3',
    },
    {
      name: 'ApproveCheckout',
      requiredPermission: 'payments.checkout.approve',
      tier: 'T3',
    },
  ],
  commands: ['payments.status.read', 'payments.checkout.create', 'payments.checkout.approve'],
};
