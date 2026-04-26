using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("ApartmentUsers", Schema = "dbo")]
public sealed class ApartmentUser
{
    [Key]
    [Column("IdApartmentUser")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdApartmentUser { get; set; }

    public int ApartmentId { get; set; }

    [Column("UserId")]
    public int UserId { get; set; }

    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
