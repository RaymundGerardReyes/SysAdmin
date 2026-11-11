import { FeatureManifest } from '../_template/feature.manifest';

export const zohoExpenseManifest: FeatureManifest = {
  id: 'zoho-expense',
  name: 'Zoho Expense',
  owner: '@zoho-expense-team',
  maxTier: 'T2',
  routes: [
    {
      name: 'ExpenseOverview',
      requiredPermission: 'zoho.expense.read',
      tier: 'T0',
    },
    {
      name: 'ExpenseCreate',
      requiredPermission: 'zoho.expense.create',
      tier: 'T2',
    },
  ],
  commands: ['zoho.expense.read', 'zoho.expense.create'],
};
