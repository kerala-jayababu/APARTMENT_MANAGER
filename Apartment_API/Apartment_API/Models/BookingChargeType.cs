using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("BookingChargeTypes", Schema = "dbo")]
public sealed class BookingChargeType
{
    [Key]
    [Column("IdBookingChargeType")]
    public int IdBookingChargeType { get; set; }

    [Required, MaxLength(30)]
    public string ChargeTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ChargeTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
