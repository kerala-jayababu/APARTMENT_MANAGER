using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("UtilityBillBatches", Schema = "dbo")]
public sealed class UtilityBillBatch
{
    [Key, Column("IdUtilityBillBatch")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUtilityBillBatch { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(30)]
    public string BatchCode { get; set; } = string.Empty;

    public int UtilityTypeId { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillingPeriodStart { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillingPeriodEnd { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    [Precision(14, 2)]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsPosted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ApplyToBlock { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ApplyToScope { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? BillingMethod { get; set; }
}
