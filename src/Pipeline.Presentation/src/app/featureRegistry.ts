import { sessionManifest } from '../features/session/feature.manifest';
import { dashboardManifest } from '../features/dashboard/feature.manifest';
import { jobsManifest } from '../features/jobs/feature.manifest';
import { sheetsManifest } from '../features/sheets/feature.manifest';
import { gmailManifest } from '../features/gmail/feature.manifest';
import { paymentsManifest } from '../features/payments/feature.manifest';
import { diagnosticsManifest } from '../features/diagnostics/feature.manifest';
import { zohoExpenseManifest } from '../features/zoho-expense/feature.manifest';
import { biginManifest } from '../features/bigin/feature.manifest';
import { chatManifest } from '../features/chat/feature.manifest';

export const FEATURE_REGISTRY = [
  sessionManifest,
  dashboardManifest,
  jobsManifest,
  sheetsManifest,
  gmailManifest,
  paymentsManifest,
  zohoExpenseManifest,
  biginManifest,
  chatManifest,
  diagnosticsManifest,
] as const;
