using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("AccountGroups", Schema = "dbo")]
public sealed class AccountGroup
{
    [Key, Column("IDAccountGroup")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAccountGroup { get; set; }

    public int? ApartmentId { get; set; }

    [Required, MaxLength(40)]
    public string GroupCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    public int? ParentGroupId { get; set; }

    [Required, MaxLength(1)]
    [MinLength(1)]
    public string AccountType { get; set; } = string.Empty;

    [Required, MaxLength(1)]
    [MinLength(1)]
    public string NormalBalance { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? ReportSection { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedByUserId { get; set; }
}
