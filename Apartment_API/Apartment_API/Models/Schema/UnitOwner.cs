using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("UnitOwners", Schema = "dbo")]
public sealed class UnitOwner
{
    [Key, Column("IdUnitOwner")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUnitOwner { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    public int PersonId { get; set; }
    public bool IsPrimaryOwner { get; set; }

    [Column(TypeName = "date")]
    public DateTime OwnershipFromDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? OwnershipToDate { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Precision(18, 2)]
    public decimal? TransferValue { get; set; }

    [Precision(5, 2)]
    public decimal? OwnershipSharePct { get; set; }
}
