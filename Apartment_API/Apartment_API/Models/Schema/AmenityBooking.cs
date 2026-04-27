using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("AmenityBookings", Schema = "dbo")]
public sealed class AmenityBooking
{
    [Key, Column("IdAmenityBooking")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAmenityBooking { get; set; }

    public int ApartmentId { get; set; }
    public int AmenityId { get; set; }
    public int UnitId { get; set; }
    public int PersonId { get; set; }

    [Column(TypeName = "date")]
    public DateTime BookingDate { get; set; }

    [Column(TypeName = "time")]
    public TimeSpan SlotStartTime { get; set; }

    [Column(TypeName = "time")]
    public TimeSpan SlotEndTime { get; set; }

    [Precision(10, 2)]
    public decimal ChargeAmount { get; set; }
    public int? InvoiceId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public int? StatusId { get; set; }
}
