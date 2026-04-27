using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Amenities", Schema = "dbo")]
public sealed class Amenity
{
    [Key, Column("IdAmenity")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAmenity { get; set; }

    public int ApartmentId { get; set; }
    public int AmenityTypeId { get; set; }
    public int IncomeHeadId { get; set; }

    [Required, MaxLength(150)]
    public string AmenityName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public short? Capacity { get; set; }
    public int ChargeTypeId { get; set; }

    [Precision(10, 2)]
    public decimal ChargeAmount { get; set; }

    public byte AdvanceBookingDays { get; set; }
    public int MaxContinuousSessionsAllowed { get; set; }

    [MaxLength(2000)]
    public string? Rules { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
