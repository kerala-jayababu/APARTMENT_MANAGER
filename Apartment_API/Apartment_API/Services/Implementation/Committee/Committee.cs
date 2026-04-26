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

    public static int DaysRemaining(CommitteeTenure t, DateTime today)
    {
        if (!t.IsActive) return 0;
        return Math.Max(0, (t.TenureEndDate.Date - today).Days);
    }

    public static bool IsSingletonRoleCode(string? code) =>
        code is not null && (
            string.Equals(code, RolePresident, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, RoleSecretary, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, RoleTreasurer, StringComparison.OrdinalIgnoreCase));
}
