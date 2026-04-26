using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("ExpenseHeads", Schema = "dbo")]
public sealed class ExpenseHead
{
    [Key]
    [Column("IdExpenseHead")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdExpenseHead { get; set; }

    public int? ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string HeadCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string HeadName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    [Column("LedgerAccountID")]
    public int? LedgerAccountId { get; set; }
}
