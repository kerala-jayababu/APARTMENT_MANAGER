using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("PriorityLevels", Schema = "dbo")]
public sealed class PriorityLevel
{
    [Key]
    [Column("IdPriorityLevel")]
    public int IdPriorityLevel { get; set; }

    [Required, MaxLength(20)]
    public string PriorityCode { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string PriorityName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
