using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("OwnershipHistory", Schema = "dbo")]
public sealed class OwnershipHistory
{
    [Key, Column("IdOwnershipHistory")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdOwnershipHistory { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    [Column("PreviousPrimaryPersonId")]
    public int? PreviousOwnerPersonId { get; set; }

    [Column("NewPrimaryPersonId")]
    public int NewOwnerPersonId { get; set; }

    [MaxLength(20)]
    public string TransferType { get; set; } = "Sale";

    [Column(TypeName = "date")]
    public DateTime TransferDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EffectiveDate { get; set; }

    [Column("DeedReference")]
    [MaxLength(100)]
    public string? SaleDeedReference { get; set; }

    [Precision(14, 2)]
    public decimal? TransferValue { get; set; }

    public int? DeedDocumentId { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [Column("CreatedByUserId")]
    public int RecordedByUserId { get; set; }

    [Column("CreatedAt")]
    public DateTime RecordedAt { get; set; }
}
