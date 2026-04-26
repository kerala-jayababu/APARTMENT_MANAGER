using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("IncomeHeads", Schema = "dbo")]
public sealed class IncomeHead
{
    [Key]
    [Column("IdIncomeHead")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdIncomeHead { get; set; }

    public int? ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string HeadCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string HeadName { get; set; } = string.Empty;

    public bool IsAutoInvoiced { get; set; }
    public byte SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    [Column("LedgerAccountID")]
    public int? LedgerAccountId { get; set; }
}
