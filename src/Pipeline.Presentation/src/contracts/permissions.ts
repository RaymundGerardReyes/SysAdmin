// Rule: L-02 One concept, one home
export const PERMISSIONS = [
  'jobs.read',
  'jobs.cancel',
  'sheets.values.read',
  'sheets.values.write',
  'gmail.messages.send',
  'chat.messages.create',
  'zoho.expense.read',
  'zoho.expense.create',
  'bigin.contacts.read',
  'bigin.contacts.create',
  'payments.status.read',
  'payments.checkout.create',
  'payments.checkout.approve',
  'integrations.connect',
  'integrations.disconnect',
  'admin.users.manage',
  'admin.policy.update',
  'admin.audit.read',
] as const;

export type Permission = typeof PERMISSIONS[number];
