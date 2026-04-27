using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("ApprovalFlows", Schema = "dbo")]
public sealed class ApprovalFlow
{
    [Key, Column("IdApprovalFlow")]
    public int IdApprovalFlow { get; set; }

    [MaxLength(50)]
    public string? EntityCode { get; set; }

    [MaxLength(50)]
    public string? EntityName { get; set; }

    [MaxLength(50)]
    public string? FlowCode { get; set; }
}
