using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("ExpenseStatuses", Schema = "dbo")]
public sealed class ExpenseStatus
{
    [Key]
    [Column("IdExpenseStatus")]
    public int IdExpenseStatus { get; set; }

    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
