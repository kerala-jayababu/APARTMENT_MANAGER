using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("FiscalYears", Schema = "dbo")]
public sealed class FiscalYear
{
    [Key, Column("IdFiscalYear")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdFiscalYear { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string FyCode { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    public bool IsCurrent { get; set; }
    public bool IsLocked { get; set; }
    public bool? IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
