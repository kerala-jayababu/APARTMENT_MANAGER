using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Apartments", Schema = "dbo")]
public sealed class Apartment
{
    [Key]
    [Column("IdApartment")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdApartment { get; set; }

    [Required, MaxLength(20)]
    public string ApartmentCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ApartmentName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string AssociationName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string PinCode { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Country { get; set; } = "India";

    [Precision(10, 7)]
    public decimal? GpsLatitude { get; set; }

    [Precision(10, 7)]
    public decimal? GpsLongitude { get; set; }

    public short TotalUnits { get; set; }
    public short TotalFloors { get; set; }
    public short? TotalBlocks { get; set; }

    [MaxLength(100)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(20)]
    public string? GstNumber { get; set; }

    [MaxLength(20)]
    public string? PanNumber { get; set; }

    public byte FiscalYearStartMonth { get; set; } = 4;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? CurrencyCode { get; set; }

    [Precision(18, 2)]
    public decimal? LateFeePercent { get; set; }

    public int? PasswordExpiryDays { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ReconApprovalRoute { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ReconciliationPeriod { get; set; }

    public int? SessionTimeoutMinutes { get; set; }

    [MaxLength(3)]
    [MinLength(3)]
    public string BaseCurrencyCode { get; set; } = "INR";

    [MaxLength(1)]
    [MinLength(1)]
    public string AccountingBasis { get; set; } = "A";

    [Column(TypeName = "date")]
    public DateTime? MigrationCutoverDate { get; set; }

    public bool MigrationCompleted { get; set; }
    public DateTime? MigrationCompletedOn { get; set; }

    [MaxLength(3)]
    [MinLength(3)]
    public string DefaultDepreciationMethod { get; set; } = "SLM";
}
