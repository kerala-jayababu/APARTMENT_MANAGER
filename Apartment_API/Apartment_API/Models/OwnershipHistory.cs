using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("OwnershipHistory", Schema = "dbo")]
public sealed class OwnershipHistory
{
    [Key, Column("IdOwnershipHistory")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdOwnershipHistory { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    public int? PreviousOwnerPersonId { get; set; }
    public int NewOwnerPersonId { get; set; }

    [MaxLength(20)]
    public string TransferType { get; set; } = "Sale";

    [Column(TypeName = "date")]
    public DateTime TransferDate { get; set; }

    [MaxLength(100)]
    public string? SaleDeedReference { get; set; }

    [Precision(14, 2)]
    public decimal? TransferValue { get; set; }

    public int? DeedDocumentId { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public int RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; }
}
