using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("UnitMMCDetails", Schema = "dbo")]
public sealed class UnitMmcDetail
{
    [Key, Column("IdUnitMMCDetail")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUnitMmcDetail { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    public int? IdMmcPeriod { get; set; }
    public int IncomeHeadId { get; set; }

    [Precision(10, 2)]
    public decimal MmcAmount { get; set; }

    [Column(TypeName = "date")]
    public DateTime MmcPeriodFrom { get; set; }

    [Column(TypeName = "date")]
    public DateTime? MmcPeriodTo { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
