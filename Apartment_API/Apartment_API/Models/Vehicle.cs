using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("Vehicles", Schema = "dbo")]
public sealed class Vehicle
{
    [Key, Column("IdVehicle")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdVehicle { get; set; }

    public int ApartmentId { get; set; }
    public int PersonId { get; set; }

    [Required, MaxLength(20)]
    public string VehicleNumber { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? Make { get; set; }

    [MaxLength(40)]
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
