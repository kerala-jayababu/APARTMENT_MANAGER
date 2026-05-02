namespace Apartment_API.DTO;

/// <summary>Unified inbox row for Approvals screen (MMC, Budget, Amenity booking).</summary>
public sealed class ApprovalInboxItemDto
{
    /// <summary>Discriminator: SET_MMC, BUDGET_HEADER, BUDGET_REVISION, AMENITY_BOOKING.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Primary entity id for review/approve APIs (per Kind).</summary>
    public int SourceId { get; init; }

    /// <summary>UI reference like AR-2026-0007 (generated until a central registry exists).</summary>
    public string ApprovalReference { get; init; } = string.Empty;

    /// <summary>Secondary code where applicable (e.g. BK-2026-0042).</summary>
    public string? SecondaryReference { get; init; }

    /// <summary>Badge label: Set MMC, Budget, Booking.</summary>
    public string TypeLabel { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    /// <summary>Display name or role for submitter.</summary>
    public string SubmittedBy { get; init; } = string.Empty;

    public DateTime SubmittedAt { get; init; }

    /// <summary>Human summary of who must approve (from ApprovalFlows.FlowCode).</summary>
    public ApprovalApproverHintDto Approver { get; init; } = new();

    /// <summary>Amount in INR where meaningful; null otherwise.</summary>
    public decimal? Amount { get; init; }

    /// <summary>Whole days since submission (UTC date).</summary>
    public int AgingDays { get; init; }

    /// <summary>Display status e.g. Pending, Pending L1.</summary>
    public string StatusLabel { get; init; } = string.Empty;

    /// <summary>Optional stage for multi-step flows when persisted later.</summary>
    public string? WorkflowStage { get; init; }
}

public sealed class ApprovalApproverHintDto
{
    /// <summary>Raw flow codes e.g. S,P.</summary>
    public string FlowCode { get; init; } = string.Empty;

    /// <summary>Short label for chips e.g. Secretary, President, Both.</summary>
    public string Summary { get; init; } = string.Empty;
}

public sealed class ApprovalsInboxSummaryDto
{
    public int TotalPending { get; init; }
    public int SetMmc { get; init; }
    public int Budget { get; init; }
    public int AmenityBookings { get; init; }
    public int AgingOver3Days { get; init; }
}
