using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("PaymentModes", Schema = "dbo")]
public sealed class PaymentMode
{
    [Key]
    [Column("IdPaymentMode")]
    public int IdPaymentMode { get; set; }

    [Required, MaxLength(30)]
    public string PaymentModeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PaymentModeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
