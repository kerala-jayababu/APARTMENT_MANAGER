using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("TenantAssignments", Schema = "dbo")]
public sealed class TenantAssignment
{
    [Key, Column("IdTenantAssignment")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTenantAssignment { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    public int PersonId { get; set; }

    [Column(TypeName = "date")]
    public DateTime LeaseStartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? LeaseEndDate { get; set; }

    [Precision(12, 2)]
    public decimal? MonthlyRent { get; set; }

    [Precision(12, 2)]
    public decimal? SecurityDeposit { get; set; }

    [MaxLength(500)]
    public string? AgreementDocUrl { get; set; }

    [Column(TypeName = "date")]
    public DateTime? VacatedDate { get; set; }

    [MaxLength(500)]
    public string? VacateRemarks { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
