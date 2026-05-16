using Apartment_API.Models;

namespace Apartment_API.Services.Implementation.Committee;

/// <summary>MC committee module — shared formatting and status/role rules.</summary>
internal static class Committee
{
    public const string StatusActive = "ACTIVE";
    public const string StatusResigned = "RESIGNED";
    public const string StatusRemoved = "REMOVED";
    public const string RolePresident = "PRESIDENT";
    public const string RoleSecretary = "SECRETARY";
    public const string RoleTreasurer = "TREASURER";

    public static string FormatTenureName(CommitteeTenure t) =>
        !string.IsNullOrWhiteSpace(t.Notes) ? t.Notes!.Trim() : $"MC Term {t.TenureStartDate.Year}–{t.TenureEndDate.Year}";

    /// <summary>True when today falls within the tenure date range (inclusive).</summary>
    public static bool IsTenureCurrent(DateTime tenureStartDate, DateTime tenureEndDate, DateTime today) =>
        tenureStartDate.Date <= today && tenureEndDate.Date >= today;

    public static bool IsTenureCurrent(CommitteeTenure t, DateTime today) =>
        IsTenureCurrent(t.TenureStartDate, t.TenureEndDate, today);

    public static int DaysRemaining(CommitteeTenure t, DateTime today) =>
        IsTenureCurrent(t, today) ? Math.Max(0, (t.TenureEndDate.Date - today).Days) : 0;

    public static bool IsSingletonRoleCode(string? code) =>
        code is not null && (
            string.Equals(code, RolePresident, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, RoleSecretary, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, RoleTreasurer, StringComparison.OrdinalIgnoreCase));
}
