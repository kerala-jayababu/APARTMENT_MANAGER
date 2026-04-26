using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("Blocks", Schema = "dbo")]
public sealed class Block
{
    [Key, Column("IdBlock")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdBlock { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string BlockCode { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string BlockName { get; set; } = string.Empty;

    public byte TotalFloors { get; set; }
    public short TotalUnits { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
