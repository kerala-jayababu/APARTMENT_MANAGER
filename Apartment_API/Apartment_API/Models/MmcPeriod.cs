using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("MMCPeriods", Schema = "dbo")]
public sealed class MmcPeriod
{
    [Key, Column("IdMMCPeriod")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdMmcPeriod { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string PeriodCode { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string PeriodName { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
