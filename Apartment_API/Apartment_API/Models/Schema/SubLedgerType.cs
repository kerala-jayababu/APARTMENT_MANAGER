using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("SubLedgerTypes", Schema = "dbo")]
public sealed class SubLedgerType
{
    [Key, Column("IDSubLedgerType")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdSubLedgerType { get; set; }

    [Required, MaxLength(30)]
    public string TypeCode { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string TypeName { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string ReferencedTable { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string ReferencedKeyColumn { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? CreatedByUserId { get; set; }
}
