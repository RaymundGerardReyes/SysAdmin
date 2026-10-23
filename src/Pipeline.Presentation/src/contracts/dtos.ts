// Rules: L-01 Layer 0 contracts, L-02 Single source of truth mirroring Pipeline.Contracts DTOs
import { JobStatus } from './jobStatus';

// ============================================================================
// 1. Google Sheets Contracts (UseCaseRequests.cs / UseCaseResponses.cs)
// ============================================================================

export interface ReadSheetRangeRequest {
  readonly spreadsheetId: string;
  readonly range: string;
}

export interface SheetRangeResponse {
  readonly range: string;
  readonly values: readonly (readonly string[])[];
}

export interface WriteSheetRangeRequest {
  readonly spreadsheetId: string;
  readonly range: string;
  readonly values: readonly (readonly string[])[];
  readonly idempotencyKey?: string;
}

export interface SheetWriteResponse {
  readonly updatedRows: number;
  readonly updatedColumns: number;
  readonly updatedCells: number;
}

// ============================================================================
// 2. Google Gmail Contracts (UseCaseRequests.cs / UseCaseResponses.cs)
// ============================================================================

export interface SendGmailMessageRequest {
  readonly recipientEmail: string;
  readonly subject: string;
  readonly body: string;
  readonly idempotencyKey?: string;
}

export interface GmailMessageResponse {
  readonly messageId: string;
  readonly threadId: string;
  readonly sentAtUtc: string;
}

// ============================================================================
// 3. PayMongo Payments Contracts (UseCaseRequests.cs / UseCaseResponses.cs)
// ============================================================================

export interface CreatePaymentCheckoutRequest {
  readonly accountId: string;
  readonly amount: number;
  readonly currency: string;
  readonly reference?: string;
  readonly idempotencyKey?: string;
}

export interface PaymentCheckoutResponse {
  readonly checkoutId: string;
  readonly status: string;
  readonly checkoutUrl?: string | null;
}

export interface ApprovePaymentCheckoutRequest {
  readonly approvalId: string;
  readonly checkoutId: string;
}

export interface PaymentApprovalResponse {
  readonly checkoutId: string;
  readonly approved: boolean;
  readonly approverUserId: string;
  readonly approvedAtUtc: string;
}

// ============================================================================
// 4. Admin & Jobs Contracts (UseCaseRequests.cs / UseCaseResponses.cs)
// ============================================================================

export interface ManageUserRolesRequest {
  readonly targetUserId: string;
  readonly newRoleNames: readonly string[];
}

export interface ManageUserRolesResponse {
  readonly targetUserId: string;
  readonly assignedRoles: readonly string[];
  readonly updatedAtUtc: string;
}

export interface JobRecordContract {
  readonly id: string;
  readonly command: string;
  readonly status: JobStatus;
  readonly updatedAtUtc: string;
  readonly correlationId: string;
}

// ============================================================================
// 5. Zoho Bigin Contracts (UseCaseRequests.cs / BiginNotificationRequests.cs / Responses)
// ============================================================================

export interface GetBiginModulesRequest {
  readonly dataCenter?: string | null;
}

export interface BiginModuleItemResponse {
  readonly id: string;
  readonly apiName: string;
  readonly moduleName: string;
  readonly singularLabel: string;
  readonly pluralLabel: string;
  readonly apiSupported: boolean;
  readonly creatable: boolean;
  readonly deletable: boolean;
  readonly editable: boolean;
  readonly viewable: boolean;
  readonly visible: boolean;
  readonly visibility: number;
  readonly generatedType: string;
  readonly sequenceNumber: number;
  readonly quickCreate: boolean;
}

export interface BiginModulesResponseContract {
  readonly modules: readonly BiginModuleItemResponse[];
}

export interface GetBiginOrgRequest {
  readonly dataCenter?: string | null;
}

export interface BiginOrgDetailsContract {
  readonly id: string;
  readonly companyName?: string | null;
  readonly domainName?: string | null;
  readonly timeZone?: string | null;
  readonly currency?: string | null;
  readonly currencySymbol?: string | null;
  readonly primaryEmail?: string | null;
  readonly phone?: string | null;
  readonly country?: string | null;
  readonly city?: string | null;
  readonly mcStatus: boolean;
  readonly paid: boolean;
  readonly paidType?: string | null;
  readonly usersLicensePurchased?: number | null;
}

export interface BiginOrgResponseContract {
  readonly org: readonly BiginOrgDetailsContract[];
}

export interface UploadBiginOrgPhotoRequest {
  readonly dataCenter?: string | null;
  readonly fileName: string;
  readonly contentType?: string;
}

export interface BiginPhotoUploadResponseContract {
  readonly code: string;
  readonly message: string;
  readonly status: string;
}

export interface GetBiginRelatedRecordsRequest {
  readonly moduleApiName: string;
  readonly recordId: string;
  readonly relatedListApiName: string;
  readonly fields?: readonly string[] | null;
  readonly page?: number | null;
  readonly perPage?: number | null;
  readonly dataCenter?: string | null;
}

export interface BiginRelatedRecordsResponseContract<TRecord = Record<string, unknown>> {
  readonly records: readonly TRecord[];
  readonly page: number;
  readonly perPage: number;
  readonly count: number;
  readonly moreRecords: boolean;
}

export interface BiginChannelSubscription {
  readonly channelId: string;
  readonly events?: readonly string[] | null;
  readonly notifyUrl?: string | null;
  readonly channelExpiry?: string | null;
  readonly deleteEvents?: boolean | null;
}

export interface EnableBiginNotificationsRequest {
  readonly dataCenter: string;
  readonly channels: readonly BiginChannelSubscription[];
}

export interface UpdateBiginNotificationsRequest {
  readonly dataCenter: string;
  readonly channels: readonly BiginChannelSubscription[];
}

export interface UpdateSpecificBiginNotificationsRequest {
  readonly dataCenter: string;
  readonly channels: readonly BiginChannelSubscription[];
}

export interface DisableBiginNotificationsRequest {
  readonly dataCenter: string;
  readonly channelIds: readonly string[];
}

export interface DisableSpecificBiginNotificationsRequest {
  readonly dataCenter: string;
  readonly channels: readonly BiginChannelSubscription[];
}

export interface GetBiginNotificationDetailsRequest {
  readonly dataCenter: string;
  readonly channelId?: string | null;
  readonly module?: string | null;
  readonly page?: number | null;
  readonly perPage?: number | null;
}

export interface BiginEventSubscriptionResponse {
  readonly channelExpiry?: string | null;
  readonly resourceUri?: string | null;
  readonly resourceId?: string | null;
  readonly resourceName?: string | null;
  readonly channelId?: string | null;
}

export interface BiginWatchChannelResponse {
  readonly code: string;
  readonly status: string;
  readonly message: string;
  readonly events?: readonly BiginEventSubscriptionResponse[] | null;
  readonly channelId?: string | null;
  readonly resourceUri?: string | null;
  readonly resourceId?: string | null;
}

export interface BiginWatchOperationResponse {
  readonly watch: readonly BiginWatchChannelResponse[];
}

export interface BiginNotificationDetailsContract {
  readonly channelId: string;
  readonly resourceName?: string | null;
  readonly resourceUri?: string | null;
  readonly notifyUrl?: string | null;
  readonly events?: readonly string[] | null;
  readonly channelExpiry?: string | null;
}

export interface BiginNotificationPageInfoContract {
  readonly perPage: number;
  readonly count: number;
  readonly page: number;
  readonly moreRecords: boolean;
}

export interface GetBiginNotificationDetailsResponseContract {
  readonly watch: readonly BiginNotificationDetailsContract[];
  readonly info: BiginNotificationPageInfoContract;
}

export interface BiginContactItemContract {
  readonly id: string;
  readonly fullName: string;
  readonly email: string;
  readonly accountName: string;
  readonly title: string;
  readonly updatedAtUtc: string;
}

export interface ReadBiginContactsRequest {
  readonly dataCenter?: string;
  readonly page?: number;
  readonly perPage?: number;
}

export interface ReadBiginContactsResponse {
  readonly contacts: readonly BiginContactItemContract[];
  readonly org: BiginOrgDetailsContract;
  readonly modules: readonly BiginModuleItemResponse[];
  readonly notifications: GetBiginNotificationDetailsResponseContract;
}

export interface CreateBiginContactRequest {
  readonly fullName: string;
  readonly email: string;
  readonly accountName: string;
  readonly title?: string;
  readonly dataCenter?: string;
}

export interface CreateBiginContactResponse {
  readonly contact: BiginContactItemContract;
  readonly code: string;
  readonly status: string;
  readonly message: string;
}

// ============================================================================
// 6. Google Chat Contracts
// ============================================================================

export interface ChatMessageContract {
  readonly messageId: string;
  readonly spaceId: string;
  readonly sender: string;
  readonly text: string;
  readonly threadKey: string;
  readonly createdAtUtc: string;
}

export interface CreateChatMessageRequest {
  readonly spaceId: string;
  readonly text: string;
  readonly threadKey?: string;
}

export interface CreateChatMessageResponse {
  readonly message: ChatMessageContract;
  readonly history: readonly ChatMessageContract[];
}

// ============================================================================
// 7. Zoho Expense Contracts
// ============================================================================

export interface ZohoExpenseItemContract {
  readonly expenseId: string;
  readonly orgId: string;
  readonly merchant: string;
  readonly category: string;
  readonly amount: number;
  readonly currency: string;
  readonly status: 'Approved' | 'Submitted' | 'Reimbursed';
  readonly createdAtUtc: string;
}

export interface ReadZohoExpensesRequest {
  readonly orgId?: string;
}

export interface ReadZohoExpensesResponse {
  readonly expenses: readonly ZohoExpenseItemContract[];
  readonly orgId: string;
  readonly totalAmount: number;
  readonly currency: string;
}

export interface CreateZohoExpenseRequest {
  readonly orgId: string;
  readonly merchant: string;
  readonly category: string;
  readonly amount: number;
  readonly currency: string;
}

export interface CreateZohoExpenseResponse {
  readonly expense: ZohoExpenseItemContract;
  readonly expenses: readonly ZohoExpenseItemContract[];
}

