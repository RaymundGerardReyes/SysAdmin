// Rules: L-04 Bridge layer, B-01 Closed envelope, L-02 Contract-faithful responses
import {
  BiginContactItemContract,
  BiginModuleItemResponse,
  BiginOrgDetailsContract,
  ChatMessageContract,
  CreateBiginContactRequest,
  CreateBiginContactResponse,
  CreateChatMessageRequest,
  CreateChatMessageResponse,
  CreatePaymentCheckoutRequest,
  CreateZohoExpenseRequest,
  CreateZohoExpenseResponse,
  GetBiginNotificationDetailsResponseContract,
  GmailMessageResponse,
  JobRecordContract,
  PaymentApprovalResponse,
  PaymentCheckoutResponse,
  ReadBiginContactsResponse,
  ReadSheetRangeRequest,
  ReadZohoExpensesResponse,
  SheetRangeResponse,
  SheetWriteResponse,
  WriteSheetRangeRequest,
  ZohoExpenseItemContract,
} from '../../contracts/dtos';
import { BridgeEnvelope, BridgeResponse } from '../bridge.types';
import { BridgeTransport } from './Transport';

const DEFAULT_SHEET_VALUES: readonly (readonly string[])[] = [
  ['Metric', 'Owner', 'Status', 'Q4 Target'],
  ['Settlement Latency (p95)', 'payments-ops@pipeline.local', 'Healthy', '< 250ms'],
  ['Bigin Webhook Watch', 'crm-sync@pipeline.local', 'Active', '99.98%'],
  ['Ledger Reconciliation', 'finance-audit@pipeline.local', 'Verified', '100.00%'],
];

const DEFAULT_BIGIN_ORG: BiginOrgDetailsContract = {
  id: 'zoho_org_7819204',
  companyName: 'Pipeline Infrastructure Holdings',
  domainName: 'pipeline-infra.local',
  timeZone: 'Asia/Manila',
  currency: 'PHP',
  currencySymbol: '₱',
  primaryEmail: 'ops-director@pipeline.local',
  phone: '+63-2-8555-0192',
  country: 'PH',
  city: 'Taguig',
  mcStatus: true,
  paid: true,
  paidType: 'Express Enterprise',
  usersLicensePurchased: 25,
};

const DEFAULT_BIGIN_MODULES: readonly BiginModuleItemResponse[] = [
  {
    id: 'mod_contacts_01',
    apiName: 'Contacts',
    moduleName: 'Contacts',
    singularLabel: 'Contact',
    pluralLabel: 'Contacts',
    apiSupported: true,
    creatable: true,
    deletable: true,
    editable: true,
    viewable: true,
    visible: true,
    visibility: 1,
    generatedType: 'default',
    sequenceNumber: 1,
    quickCreate: true,
  },
  {
    id: 'mod_pipelines_02',
    apiName: 'Pipelines',
    moduleName: 'Pipelines',
    singularLabel: 'Deal',
    pluralLabel: 'Deals',
    apiSupported: true,
    creatable: true,
    deletable: true,
    editable: true,
    viewable: true,
    visible: true,
    visibility: 1,
    generatedType: 'default',
    sequenceNumber: 2,
    quickCreate: true,
  },
  {
    id: 'mod_accounts_03',
    apiName: 'Accounts',
    moduleName: 'Companies',
    singularLabel: 'Company',
    pluralLabel: 'Companies',
    apiSupported: true,
    creatable: true,
    deletable: false,
    editable: true,
    viewable: true,
    visible: true,
    visibility: 1,
    generatedType: 'default',
    sequenceNumber: 3,
    quickCreate: true,
  },
];

const DEFAULT_BIGIN_NOTIFICATIONS: GetBiginNotificationDetailsResponseContract = {
  watch: [
    {
      channelId: '1000018842',
      resourceName: 'Contacts',
      resourceUri: 'https://www.zohoapis.com/bigin/v2/Contacts',
      notifyUrl: 'https://hooks.pipeline.local/bigin/contacts',
      events: ['Contacts.create', 'Contacts.edit'],
      channelExpiry: '2026-11-01T00:00:00Z',
    },
    {
      channelId: '1000018843',
      resourceName: 'Pipelines',
      resourceUri: 'https://www.zohoapis.com/bigin/v2/Pipelines',
      notifyUrl: 'https://hooks.pipeline.local/bigin/pipelines',
      events: ['Pipelines.all'],
      channelExpiry: '2026-11-01T00:00:00Z',
    },
  ],
  info: {
    perPage: 200,
    count: 2,
    page: 1,
    moreRecords: false,
  },
};

export class MockTransport implements BridgeTransport {
  private handlers = new Map<string, (env: BridgeEnvelope<unknown>) => Promise<BridgeResponse<unknown>>>();
  private readonly simulatedLatencyMs: number;

  private sheetRanges = new Map<string, readonly (readonly string[])[]>([
    ['ops-ledger-2026:Summary!A1:D10', DEFAULT_SHEET_VALUES],
  ]);

  private biginContacts: BiginContactItemContract[] = [
    {
      id: 'bigin_cnt_901',
      fullName: 'Elena Rostova',
      email: 'elena.rostova@acme-logistics.io',
      accountName: 'Acme Logistics APAC',
      title: 'VP of Treasury Operations',
      updatedAtUtc: '2026-10-23T08:15:00Z',
    },
    {
      id: 'bigin_cnt_902',
      fullName: 'Marcus Vance',
      email: 'm.vance@nexus-fintech.ph',
      accountName: 'Nexus Fintech Group',
      title: 'Head of Settlement Compliance',
      updatedAtUtc: '2026-10-23T09:40:00Z',
    },
    {
      id: 'bigin_cnt_903',
      fullName: 'Sora Takahashi',
      email: 'stakahashi@kaito-ventures.jp',
      accountName: 'Kaito Cloud Ventures',
      title: 'Principal Partner',
      updatedAtUtc: '2026-10-23T11:05:00Z',
    },
  ];

  private chatHistory: ChatMessageContract[] = [
    {
      messageId: 'spaces/ops-alerts/messages/msg_401',
      spaceId: 'spaces/ops-alerts',
      sender: 'release-bot@pipeline.local',
      text: '[AUTHZ_P01..P11] Daily capability ticket verification completed across 6 connectors.',
      threadKey: 'ops-daily-verification',
      createdAtUtc: '2026-10-23T09:00:00Z',
    },
    {
      messageId: 'spaces/ops-alerts/messages/msg_402',
      spaceId: 'spaces/ops-alerts',
      sender: 'treasury-watch@pipeline.local',
      text: 'PayMongo settlement batch #2026-10-23 reconciled with 0 discrepancies.',
      threadKey: 'settlement-reconcile',
      createdAtUtc: '2026-10-23T10:30:00Z',
    },
  ];

  private expenses: ZohoExpenseItemContract[] = [
    {
      expenseId: 'exp_zoho_3001',
      orgId: 'zoho_org_primary',
      merchant: 'Cloudflare Enterprise Edge',
      category: 'Infrastructure & Security',
      amount: 2450.0,
      currency: 'USD',
      status: 'Approved',
      createdAtUtc: '2026-10-21T14:20:00Z',
    },
    {
      expenseId: 'exp_zoho_3002',
      orgId: 'zoho_org_primary',
      merchant: 'GitHub Enterprise Cloud',
      category: 'Developer Tooling',
      amount: 1290.0,
      currency: 'USD',
      status: 'Reimbursed',
      createdAtUtc: '2026-10-22T16:05:00Z',
    },
    {
      expenseId: 'exp_zoho_3003',
      orgId: 'zoho_org_primary',
      merchant: 'Dedicated HSM Key Vault Audit',
      category: 'Compliance & Attestation',
      amount: 3800.0,
      currency: 'USD',
      status: 'Submitted',
      createdAtUtc: '2026-10-23T07:45:00Z',
    },
  ];

  private jobs: JobRecordContract[] = [
    {
      id: 'job_sync_bigin_8821',
      command: 'bigin.contacts.read',
      status: 'Succeeded',
      updatedAtUtc: '2026-10-23T11:30:00Z',
      correlationId: 'corr_8821_bigin_sync',
    },
    {
      id: 'job_reconcile_pay_8822',
      command: 'payments.checkout.create',
      status: 'Running',
      updatedAtUtc: '2026-10-23T11:58:00Z',
      correlationId: 'corr_8822_paymongo_batch',
    },
    {
      id: 'job_export_sheets_8823',
      command: 'sheets.write',
      status: 'Pending',
      updatedAtUtc: '2026-10-23T12:02:00Z',
      correlationId: 'corr_8823_ledger_rollup',
    },
  ];

  private sequenceCounter = 1000;

  constructor(options?: { readonly simulatedLatencyMs?: number }) {
    const isTestEnv = typeof process !== 'undefined' && process.env['NODE_ENV'] === 'test';
    this.simulatedLatencyMs = options?.simulatedLatencyMs ?? (isTestEnv ? 0 : 180);
  }

  registerHandler<TReq, TRes>(
    command: string,
    handler: (envelope: BridgeEnvelope<TReq>) => Promise<BridgeResponse<TRes>>
  ): void {
    this.handlers.set(command, handler as (env: BridgeEnvelope<unknown>) => Promise<BridgeResponse<unknown>>);
  }

  private async delayIfConfigured(): Promise<void> {
    if (this.simulatedLatencyMs <= 0) {
      return;
    }
    let timerId: ReturnType<typeof setTimeout> | undefined;
    try {
      await new Promise<void>((resolve) => {
        timerId = setTimeout(resolve, this.simulatedLatencyMs);
      });
    } finally {
      if (timerId !== undefined) {
        clearTimeout(timerId);
      }
    }
  }

  async send<TReq, TRes>(envelope: BridgeEnvelope<TReq>): Promise<BridgeResponse<TRes>> {
    const handler = this.handlers.get(envelope.command);
    if (handler) {
      const res = await handler(envelope as BridgeEnvelope<unknown>);
      return res as BridgeResponse<TRes>;
    }

    await this.delayIfConfigured();

    const nextSeq = ++this.sequenceCounter;
    const nowIso = new Date().toISOString();

    switch (envelope.command) {
      case 'sheets.read': {
        const req = (envelope.payload ?? {}) as Partial<ReadSheetRangeRequest>;
        const spreadsheetId = req.spreadsheetId || 'ops-ledger-2026';
        const range = req.range || 'Summary!A1:D10';
        const key = `${spreadsheetId}:${range}`;
        const values = this.sheetRanges.get(key) ?? DEFAULT_SHEET_VALUES;
        const response: SheetRangeResponse = {
          range,
          values,
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'sheets.write': {
        const req = (envelope.payload ?? {}) as Partial<WriteSheetRangeRequest>;
        const spreadsheetId = req.spreadsheetId || 'ops-ledger-2026';
        const range = req.range || 'Summary!A1:D10';
        const values = req.values ?? DEFAULT_SHEET_VALUES;
        this.sheetRanges.set(`${spreadsheetId}:${range}`, values);
        const updatedRows = values.length;
        const updatedColumns = values[0]?.length ?? 0;
        const response: SheetWriteResponse = {
          updatedRows,
          updatedColumns,
          updatedCells: updatedRows * updatedColumns,
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'gmail.send': {
        const response: GmailMessageResponse = {
          messageId: `msg_gmail_${nextSeq}`,
          threadId: `thr_gmail_${nextSeq}`,
          sentAtUtc: nowIso,
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'payments.checkout.create': {
        const req = (envelope.payload ?? {}) as Partial<CreatePaymentCheckoutRequest>;
        const checkoutId = `chk_paymongo_${nextSeq}`;
        const response: PaymentCheckoutResponse = {
          checkoutId,
          status: `awaiting_approval (${req.currency ?? 'USD'} ${(req.amount ?? 0).toFixed(2)})`,
          checkoutUrl: `https://checkout.paymongo.com/sessions/${checkoutId}`,
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'payments.checkout.approve': {
        const req = (envelope.payload ?? {}) as { approvalId?: string; checkoutId?: string };
        const response: PaymentApprovalResponse = {
          checkoutId: req.checkoutId || `chk_paymongo_${nextSeq}`,
          approved: true,
          approverUserId: 'operator@pipeline.local',
          approvedAtUtc: nowIso,
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'jobs.read': {
        return {
          id: envelope.id,
          success: true,
          data: { jobs: [...this.jobs] } as unknown as TRes,
        };
      }

      case 'jobs.cancel': {
        const req = (envelope.payload ?? {}) as { jobId?: string };
        if (req.jobId) {
          this.jobs = this.jobs.map((job) =>
            job.id === req.jobId ? { ...job, status: 'Cancelled', updatedAtUtc: nowIso } : job
          );
        }
        return {
          id: envelope.id,
          success: true,
          data: { cancelled: true } as unknown as TRes,
        };
      }

      case 'bigin.contacts.read': {
        const response: ReadBiginContactsResponse = {
          contacts: [...this.biginContacts],
          org: DEFAULT_BIGIN_ORG,
          modules: DEFAULT_BIGIN_MODULES,
          notifications: DEFAULT_BIGIN_NOTIFICATIONS,
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'bigin.contacts.create': {
        const req = (envelope.payload ?? {}) as Partial<CreateBiginContactRequest>;
        const newContact: BiginContactItemContract = {
          id: `bigin_cnt_${nextSeq}`,
          fullName: req.fullName?.trim() || 'New Operations Contact',
          email: req.email?.trim() || `contact.${nextSeq}@pipeline.local`,
          accountName: req.accountName?.trim() || 'Pipeline Partner Account',
          title: req.title?.trim() || 'Operations Lead',
          updatedAtUtc: nowIso,
        };
        this.biginContacts = [newContact, ...this.biginContacts];
        const response: CreateBiginContactResponse = {
          contact: newContact,
          code: 'SUCCESS',
          status: 'success',
          message: 'record added',
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'chat.create': {
        const req = (envelope.payload ?? {}) as Partial<CreateChatMessageRequest>;
        const spaceId = req.spaceId?.trim() || 'spaces/ops-alerts';
        const newMessage: ChatMessageContract = {
          messageId: `${spaceId}/messages/msg_${nextSeq}`,
          spaceId,
          sender: 'operator@pipeline.local',
          text: req.text?.trim() || 'Operational status broadcast verified.',
          threadKey: req.threadKey?.trim() || `thread_${nextSeq}`,
          createdAtUtc: nowIso,
        };
        this.chatHistory = [newMessage, ...this.chatHistory];
        const response: CreateChatMessageResponse = {
          message: newMessage,
          history: [...this.chatHistory],
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'zoho.expense.read': {
        const req = (envelope.payload ?? {}) as { orgId?: string };
        const orgId = req.orgId?.trim() || 'zoho_org_primary';
        const totalAmount = this.expenses.reduce((acc, item) => acc + item.amount, 0);
        const response: ReadZohoExpensesResponse = {
          expenses: [...this.expenses],
          orgId,
          totalAmount,
          currency: 'USD',
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      case 'zoho.expense.create': {
        const req = (envelope.payload ?? {}) as Partial<CreateZohoExpenseRequest>;
        const newExpense: ZohoExpenseItemContract = {
          expenseId: `exp_zoho_${nextSeq}`,
          orgId: req.orgId?.trim() || 'zoho_org_primary',
          merchant: req.merchant?.trim() || 'Cloud Infrastructure Provider',
          category: req.category?.trim() || 'Infrastructure & Security',
          amount: typeof req.amount === 'number' && Number.isFinite(req.amount) ? req.amount : 450.0,
          currency: req.currency?.trim() || 'USD',
          status: 'Submitted',
          createdAtUtc: nowIso,
        };
        this.expenses = [newExpense, ...this.expenses];
        const response: CreateZohoExpenseResponse = {
          expense: newExpense,
          expenses: [...this.expenses],
        };
        return {
          id: envelope.id,
          success: true,
          data: response as unknown as TRes,
        };
      }

      default:
        return {
          id: envelope.id,
          success: true,
          data: { mockSuccess: true } as TRes,
        };
    }
  }
}
