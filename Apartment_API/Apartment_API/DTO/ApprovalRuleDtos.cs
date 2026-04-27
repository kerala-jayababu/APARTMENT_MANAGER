namespace Apartment_API.DTO;

public sealed class ApprovalRuleDto
{
    public int Id { get; init; }
    public string EntityCode { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public string ApprovalFlow { get; init; } = string.Empty;
}

public sealed class UpdateApprovalRuleRequest
{
    public string ApprovalFlow { get; set; } = string.Empty;
}
