using System.Globalization;
using System.Text;

namespace Apartment_API.Helpers;

/// <summary>
/// Synthetic batch id: Base64 of <c>Apt:{id}|FY:{id}|Usr:{id}|Ts:{ISO8601}</c> (spec §2.2).
/// </summary>
public static class BudgetRevisionBatchId
{
    public static string Encode(int apartmentId, int fiscalYearId, int createdBy, DateTime createdAtUtc)
    {
        var utc = createdAtUtc.Kind == DateTimeKind.Utc ? createdAtUtc : createdAtUtc.ToUniversalTime();
        var s = $"Apt:{apartmentId}|FY:{fiscalYearId}|Usr:{createdBy}|Ts:{utc:O}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    }

    public static bool TryDecode(string batchId, out int apartmentId, out int fiscalYearId, out int createdBy, out DateTime createdAtUtc)
    {
        apartmentId = 0;
        fiscalYearId = 0;
        createdBy = 0;
        createdAtUtc = default;
        if (string.IsNullOrWhiteSpace(batchId)) return false;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(batchId)));
            // Apt:12|FY:2627|Usr:99|Ts:2026-05-02T09:01:00.0000000Z
            var parts = json.Split('|', StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                var kv = p.Split(':', 2);
                if (kv.Length != 2) continue;
                var key = kv[0].Trim();
                var val = kv[1].Trim();
                switch (key)
                {
                    case "Apt":
                        if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out apartmentId)) return false;
                        break;
                    case "FY":
                        if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out fiscalYearId)) return false;
                        break;
                    case "Usr":
                        if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out createdBy)) return false;
                        break;
                    case "Ts":
                        if (!DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                            return false;
                        createdAtUtc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
                        break;
                }
            }
            return apartmentId > 0 && fiscalYearId > 0 && createdBy > 0 && createdAtUtc != default;
        }
        catch
        {
            return false;
        }
    }

    private static string PadBase64(string s)
    {
        var pad = s.Length % 4;
        return pad == 0 ? s : s + new string('=', 4 - pad);
    }
}
