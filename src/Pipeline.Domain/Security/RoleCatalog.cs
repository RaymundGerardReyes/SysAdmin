namespace Pipeline.Domain.Security;

public static class RoleCatalog
{
    public const string ViewerRole = "Viewer";
    public const string OperatorRole = "Operator";
    public const string FinanceOperatorRole = "FinanceOperator";
    public const string ApproverRole = "Approver";
    public const string IntegrationAdminRole = "IntegrationAdmin";
    public const string AuditorRole = "Auditor";
    public const string OwnerRole = "Owner";

    public static readonly Role Viewer = new(
        ViewerRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.BiginContactsRead,
            PermissionCatalog.BiginOrgRead,
            PermissionCatalog.BiginRelatedListsRead,
            PermissionCatalog.BiginNotificationsRead
        });

    public static readonly Role Operator = new(
        OperatorRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.JobsCancel,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.SheetsValuesWrite,
            PermissionCatalog.GmailMessagesSend,
            PermissionCatalog.ChatMessagesCreate,
            PermissionCatalog.BiginContactsRead,
            PermissionCatalog.BiginContactsCreate,
            PermissionCatalog.BiginOrgRead,
            PermissionCatalog.BiginOrgWrite,
            PermissionCatalog.BiginRelatedListsRead,
            PermissionCatalog.BiginNotificationsRead,
            PermissionCatalog.BiginNotificationsWrite
        });

    public static readonly Role FinanceOperator = new(
        FinanceOperatorRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.JobsCancel,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.SheetsValuesWrite,
            PermissionCatalog.ZohoExpenseRead,
            PermissionCatalog.ZohoExpenseCreate,
            PermissionCatalog.PaymentsStatusRead,
            PermissionCatalog.PaymentsCheckoutCreate
        });

    public static readonly Role Approver = new(
        ApproverRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.JobsCancel,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.ZohoExpenseRead,
            PermissionCatalog.PaymentsStatusRead,
            PermissionCatalog.PaymentsCheckoutApprove
        });

    public static readonly Role IntegrationAdmin = new(
        IntegrationAdminRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.IntegrationsConnect,
            PermissionCatalog.IntegrationsDisconnect
        });

    public static readonly Role Auditor = new(
        AuditorRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.AdminAuditRead
        });

    public static readonly Role Owner = new(
        OwnerRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.JobsCancel,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.SheetsValuesWrite,
            PermissionCatalog.GmailMessagesSend,
            PermissionCatalog.ChatMessagesCreate,
            PermissionCatalog.ZohoExpenseRead,
            PermissionCatalog.ZohoExpenseCreate,
            PermissionCatalog.BiginContactsRead,
            PermissionCatalog.BiginContactsCreate,
            PermissionCatalog.BiginOrgRead,
            PermissionCatalog.BiginOrgWrite,
            PermissionCatalog.BiginRelatedListsRead,
            PermissionCatalog.BiginNotificationsRead,
            PermissionCatalog.BiginNotificationsWrite,
            PermissionCatalog.PaymentsStatusRead,
            PermissionCatalog.IntegrationsConnect,
            PermissionCatalog.IntegrationsDisconnect,
            PermissionCatalog.AdminUsersManage,
            PermissionCatalog.AdminPolicyUpdate,
            PermissionCatalog.AdminAuditRead
        });

    public static IReadOnlyDictionary<string, Role> Baseline { get; } =
        new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase)
        {
            [ViewerRole] = Viewer,
            [OperatorRole] = Operator,
            [FinanceOperatorRole] = FinanceOperator,
            [ApproverRole] = Approver,
            [IntegrationAdminRole] = IntegrationAdmin,
            [AuditorRole] = Auditor,
            [OwnerRole] = Owner
        };

    public static bool TryGetRole(string roleName, out Role? role) =>
        Baseline.TryGetValue(roleName, out role);
}
