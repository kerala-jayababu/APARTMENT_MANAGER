using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("ExpenseMainHeads", Schema = "dbo")]
public sealed class ExpenseMainHead
{
    [Key, Column("IdExpenseMainHead")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdExpenseMainHead { get; set; }

    public int? ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string MainHeadCode { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string MainHeadName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
