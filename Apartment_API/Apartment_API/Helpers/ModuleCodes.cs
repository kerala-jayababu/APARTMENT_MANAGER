using Apartment_API.Configuration;

namespace Apartment_API.Helpers;

/// <summary>Known module codes from dbo.Modules / RolePermissions seed data.</summary>
public static class ModuleCodes
{
    public const string PlatformSetup = "M01";
    public const string UnitManagement = "M02";
    public const string ApprovalRules = "M03";
    public const string OwnersResidents = "M04";
    public const string McCommittee = "M05";
    public const string ApprovalsInbox = "APPROVALS";
    public const string IncomeHeads = "M07";
    public const string ExpenseHeads = "M08";
    public const string Budget = "M09";
    public const string Collections = "M10";
    public const string Expenses = "M11";
    public const string BankAccounts = "M11B";
    public const string Reconciliation = "M12";
    public const string PayDues = "M13";
    public const string PaymentGateway = "M13A";
    public const string Amenities = "M06";
    public const string Helpdesk = "M20";
    public const string NoticeBoard = "M21";
    public const string Documents = "M14";
    public const string Visitors = "M22";
    public const string UsersAccess = "M15";
    public const string Reports = "M16";
    public const string AuditSecurity = "M17";
    public const string Notifications = "M18";
    public const string MasterData = "M-MD";
    public const string Vendor = "VENDOR";
    public const string ChartOfAccounts = "M-A1";
    public const string JournalVouchers = "M-A2";
    public const string OpeningBalances = "M-A3";
    public const string FixedAssets = "M-A4";
    public const string FundManagement = "M-A5";
    public const string BankReconciliation = "M-A6";
    public const string FinancialReports = "M-A7";
    public const string PeriodClose = "M-A8";

    private static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [PlatformSetup] = "Platform setup",
            [UnitManagement] = "Unit management",
            [ApprovalRules] = "Approval rules",
            [OwnersResidents] = "Owners & residents",
            [McCommittee] = "MC committee",
            [ApprovalsInbox] = "Approvals inbox",
            [IncomeHeads] = "Income heads",
            [ExpenseHeads] = "Expense heads",
            [Budget] = "Budget",
            [Collections] = "Collections",
            [Expenses] = "Expenses",
            [BankAccounts] = "Bank accounts",
            [Reconciliation] = "Reconciliation",
            [PayDues] = "Pay dues",
            [PaymentGateway] = "Payment gateway",
            [Amenities] = "Amenities",
            [Helpdesk] = "Helpdesk",
            [NoticeBoard] = "Notice board",
            [Documents] = "Documents",
            [Visitors] = "Visitors / gate pass",
            [UsersAccess] = "Users & access",
            [Reports] = "Reports",
            [AuditSecurity] = "Audit & security",
            [Notifications] = "Notifications",
            [MasterData] = "Master data",
            [Vendor] = "Vendor master",
            [ChartOfAccounts] = "Chart of accounts",
            [JournalVouchers] = "Journal vouchers",
            [OpeningBalances] = "Opening balances",
            [FixedAssets] = "Fixed assets",
            [FundManagement] = "Fund management",
            [BankReconciliation] = "Bank reconciliation",
            [FinancialReports] = "Financial reports",
            [PeriodClose] = "Period close"
        };

    public static bool IsKnown(string? moduleCode) =>
        !string.IsNullOrWhiteSpace(moduleCode) && DisplayNames.ContainsKey(moduleCode.Trim());

    public static string GetDisplayName(string moduleCode) =>
        DisplayNames.TryGetValue(moduleCode.Trim(), out var name) ? name : moduleCode.Trim();

    public static string FormatActionLabel(PermissionAction action) => action switch
    {
        PermissionAction.View => "View",
        PermissionAction.Create => "Create",
        PermissionAction.Edit => "Edit",
        PermissionAction.Delete => "Delete",
        PermissionAction.Approve => "Approve",
        _ => action.ToString()
    };
}
