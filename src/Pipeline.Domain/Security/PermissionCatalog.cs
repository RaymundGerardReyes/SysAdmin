namespace Pipeline.Domain.Security;

public static class PermissionCatalog
{
    public static readonly Permission JobsRead = new("jobs.read");
    public static readonly Permission JobsCancel = new("jobs.cancel");
    public static readonly Permission SheetsValuesRead = new("sheets.values.read");
    public static readonly Permission SheetsValuesWrite = new("sheets.values.write");
    public static readonly Permission GmailMessagesSend = new("gmail.messages.send");
    public static readonly Permission ChatMessagesCreate = new("chat.messages.create");
    public static readonly Permission ZohoExpenseRead = new("zoho.expense.read");
    public static readonly Permission ZohoExpenseCreate = new("zoho.expense.create");
    public static readonly Permission BiginContactsRead = new("bigin.contacts.read");
    public static readonly Permission BiginContactsCreate = new("bigin.contacts.create");
    public static readonly Permission PaymentsStatusRead = new("payments.status.read");
    public static readonly Permission PaymentsCheckoutCreate = new("payments.checkout.create");
    public static readonly Permission PaymentsCheckoutApprove = new("payments.checkout.approve");
    public static readonly Permission IntegrationsConnect = new("integrations.connect");
    public static readonly Permission IntegrationsDisconnect = new("integrations.disconnect");
    public static readonly Permission AdminUsersManage = new("admin.users.manage");
    public static readonly Permission AdminPolicyUpdate = new("admin.policy.update");
    public static readonly Permission AdminAuditRead = new("admin.audit.read");

    public static IReadOnlyDictionary<Permission, RiskTier> All { get; } = new Dictionary<Permission, RiskTier>
    {
        [JobsRead] = RiskTier.T0,
        [JobsCancel] = RiskTier.T1,
        [SheetsValuesRead] = RiskTier.T0,
        [SheetsValuesWrite] = RiskTier.T1,
        [GmailMessagesSend] = RiskTier.T2,
        [ChatMessagesCreate] = RiskTier.T2,
        [ZohoExpenseRead] = RiskTier.T0,
        [ZohoExpenseCreate] = RiskTier.T2,
        [BiginContactsRead] = RiskTier.T0,
        [BiginContactsCreate] = RiskTier.T2,
        [PaymentsStatusRead] = RiskTier.T0,
        [PaymentsCheckoutCreate] = RiskTier.T3,
        [PaymentsCheckoutApprove] = RiskTier.T3,
        [IntegrationsConnect] = RiskTier.T3,
        [IntegrationsDisconnect] = RiskTier.T3,
        [AdminUsersManage] = RiskTier.T3,
        [AdminPolicyUpdate] = RiskTier.T3,
        [AdminAuditRead] = RiskTier.T1
    };

    public static bool Exists(Permission permission) => All.ContainsKey(permission);

    public static bool TryGetTier(Permission permission, out RiskTier tier) =>
        All.TryGetValue(permission, out tier);
}
