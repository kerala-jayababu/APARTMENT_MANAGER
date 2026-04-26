using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Vendors", Schema = "dbo")]
public sealed class Vendor
{
    [Key]
    [Column("IdVendor")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdVendor { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string VendorCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string VendorName { get; set; } = string.Empty;

    public int VendorTypeId { get; set; }

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? PhoneNumber { get; set; }

    [MaxLength(20)]
    public string? GstNumber { get; set; }

    [MaxLength(20)]
    public string? PanNumber { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(50)]
    public string? BankAccountNumber { get; set; }

    [MaxLength(15)]
    public string? IfscCode { get; set; }

    [MaxLength(200)]
    public string? AddressLine1 { get; set; }

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column("ControlLedgerAccountID")]
    public int? ControlLedgerAccountId { get; set; }

    [Precision(18, 2)]
    public decimal OpeningPayable { get; set; }
}
