// Rules: R-01 One bridge door, B-01 Closed envelope, B-07 Drop token fields, B-02 Timeout
import { PipelineBridge } from '../../src/bridge/PipelineBridge';
import { MockTransport } from '../../src/bridge/transports/MockTransport';
import { createCorrelationId } from '../../src/core/ids/correlationId';
import { createIdempotencyKey } from '../../src/core/ids/idempotencyKey';
import { isOk, isErr } from '../../src/core/result';
import {
  CreateBiginContactRequest,
  CreateBiginContactResponse,
  CreateChatMessageRequest,
  CreateChatMessageResponse,
  CreateZohoExpenseRequest,
  CreateZohoExpenseResponse,
  ReadBiginContactsResponse,
  ReadZohoExpensesResponse,
} from '../../src/contracts/dtos';

describe('PipelineBridge', () => {
  it('[R-01, B-01] sends closed envelope and receives success', async () => {
    const transport = new MockTransport();
    transport.registerHandler('sheets.read', async (env) => {
      return {
        id: env.id,
        success: true,
        data: { range: 'A1:B2', values: [['1', '2']] },
      };
    });

    const bridge = new PipelineBridge(transport);
    const correlationId = createCorrelationId();
    const result = await bridge.command('sheets.read', { spreadsheetId: 'sheet_1', range: 'A1:B2' }, correlationId);

    expect(isOk(result)).toBe(true);
    if (isOk(result)) {
      expect((result.value as any).range).toBe('A1:B2');
    }
  });

  it('[B-07] drops token-like fields from bridge responses', async () => {
    const transport = new MockTransport();
    transport.registerHandler('sheets.read', async (env) => {
      return {
        id: env.id,
        success: true,
        data: {
          range: 'A1',
          access_token: 'secret_leak_attempt',
          refresh_token: 'refresh_leak',
          client_secret: 'cs_leak',
        },
      };
    });

    const bridge = new PipelineBridge(transport);
    const correlationId = createCorrelationId();
    const result = await bridge.command('sheets.read', {}, correlationId);

    expect(isOk(result)).toBe(true);
    if (isOk(result)) {
      const data = result.value as any;
      expect(data.range).toBe('A1');
      expect(data.access_token).toBeUndefined();
      expect(data.refresh_token).toBeUndefined();
      expect(data.client_secret).toBeUndefined();
    }
  });

  it('[B-02] maps transport failure to AppError', async () => {
    const transport = new MockTransport();
    transport.registerHandler('sheets.read', async (env) => {
      return {
        id: env.id,
        success: false,
        errorCode: 'PERMISSION_MISSING',
        errorMessage: 'User lacks sheets.values.read',
        pathId: 'AUTHZ_P05',
      };
    });

    const bridge = new PipelineBridge(transport);
    const correlationId = createCorrelationId();
    const result = await bridge.command('sheets.read', {}, correlationId);

    expect(isErr(result)).toBe(true);
    if (isErr(result)) {
      expect(result.error.code).toBe('PERMISSION_MISSING');
      expect(result.error.pathId).toBe('AUTHZ_P05');
      expect(result.error.correlationId).toBe(correlationId);
    }
  });

  it('[R-01, B-01] serves contract-faithful Bigin, Chat, and Zoho Expense responses with state persistence', async () => {
    const transport = new MockTransport();
    const bridge = new PipelineBridge(transport);
    const correlationId = createCorrelationId();
    const idempotencyKey = createIdempotencyKey();

    // 1. Bigin contacts read & create
    const biginRead = await bridge.command<Record<string, never>, ReadBiginContactsResponse>(
      'bigin.contacts.read',
      {},
      correlationId
    );
    expect(isOk(biginRead)).toBe(true);
    if (isOk(biginRead)) {
      expect(biginRead.value.contacts.length).toBeGreaterThanOrEqual(3);
      expect(biginRead.value.org.companyName).toBe('Pipeline Infrastructure Holdings');
      expect(biginRead.value.modules.length).toBeGreaterThan(0);
      expect(biginRead.value.notifications.watch.length).toBeGreaterThan(0);
    }

    const newContactReq: CreateBiginContactRequest = {
      fullName: 'Carlos Santana',
      email: 'carlos.santana@global-logistics.ph',
      accountName: 'Global Logistics Hub',
      title: 'Chief Risk Officer',
    };
    const biginCreate = await bridge.command<CreateBiginContactRequest, CreateBiginContactResponse>(
      'bigin.contacts.create',
      newContactReq,
      correlationId,
      idempotencyKey
    );
    expect(isOk(biginCreate)).toBe(true);
    if (isOk(biginCreate)) {
      expect(biginCreate.value.contact.fullName).toBe('Carlos Santana');
      expect(biginCreate.value.status).toBe('success');
    }

    // 2. Chat create
    const chatReq: CreateChatMessageRequest = {
      spaceId: 'spaces/compliance-audit',
      text: 'Mandatory quarterly audit review initiated.',
      threadKey: 'audit-q4',
    };
    const chatRes = await bridge.command<CreateChatMessageRequest, CreateChatMessageResponse>(
      'chat.create',
      chatReq,
      correlationId,
      idempotencyKey
    );
    expect(isOk(chatRes)).toBe(true);
    if (isOk(chatRes)) {
      expect(chatRes.value.message.spaceId).toBe('spaces/compliance-audit');
      expect(chatRes.value.history[0]?.text).toBe('Mandatory quarterly audit review initiated.');
    }

    // 3. Zoho Expense read & create
    const expenseReq: CreateZohoExpenseRequest = {
      orgId: 'zoho_org_primary',
      merchant: 'Azure Enterprise HSM',
      category: 'Security Hardware',
      amount: 4950.0,
      currency: 'USD',
    };
    const expenseRes = await bridge.command<CreateZohoExpenseRequest, CreateZohoExpenseResponse>(
      'zoho.expense.create',
      expenseReq,
      correlationId,
      idempotencyKey
    );
    expect(isOk(expenseRes)).toBe(true);
    if (isOk(expenseRes)) {
      expect(expenseRes.value.expense.merchant).toBe('Azure Enterprise HSM');
      expect(expenseRes.value.expense.amount).toBe(4950.0);
    }

    const expenseRead = await bridge.command<Record<string, never>, ReadZohoExpensesResponse>(
      'zoho.expense.read',
      {},
      correlationId
    );
    expect(isOk(expenseRead)).toBe(true);
    if (isOk(expenseRead)) {
      expect(expenseRead.value.expenses.some((e) => e.merchant === 'Azure Enterprise HSM')).toBe(true);
      expect(expenseRead.value.totalAmount).toBeGreaterThanOrEqual(4950.0);
    }
  });
});
