using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ApprovalRuleService(AppDbContext db) : IApprovalRuleService
{
    public async Task<IReadOnlyList<ApprovalRuleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.ApprovalFlows.AsNoTracking()
            .OrderBy(x => x.IdApprovalFlow)
            .Select(x => new ApprovalRuleDto
            {
                Id = x.IdApprovalFlow,
                EntityCode = x.EntityCode ?? string.Empty,
                EntityName = x.EntityName ?? string.Empty,
                ApprovalFlow = NormalizeFlowOrThrow(x.FlowCode)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows;
    }

    public async Task UpdateAsync(int id, UpdateApprovalRuleRequest request, CancellationToken cancellationToken = default)
    {
        var row = await db.ApprovalFlows
            .FirstOrDefaultAsync(x => x.IdApprovalFlow == id, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) throw new InvalidOperationException("Approval rule not found.");
        row.FlowCode = NormalizeFlowOrThrow(request.ApprovalFlow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeFlowOrThrow(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new InvalidOperationException("ApprovalFlow is required.");

        var tokens = input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0 || tokens.Length > 2)
            throw new InvalidOperationException("ApprovalFlow must be S, P, S,P or P,S.");

        var normalized = new List<string>(2);
        foreach (var token in tokens)
        {
            var t = token.Trim().ToUpperInvariant();
            var mapped = t switch
            {
                "S" or "SECRETARY" => "S",
                "P" or "PRESIDENT" => "P",
                _ => throw new InvalidOperationException("ApprovalFlow allows only Secretary or President.")
            };
            if (!normalized.Contains(mapped))
                normalized.Add(mapped);
        }

        if (normalized.Count == 0 || normalized.Count > 2)
            throw new InvalidOperationException("ApprovalFlow must be S, P, S,P or P,S.");
        if (normalized.Count == 2 && normalized[0] == normalized[1])
            throw new InvalidOperationException("ApprovalFlow cannot repeat the same approver.");

        return string.Join(",", normalized);
    }
}
