using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("UnitStatusHistory", Schema = "dbo")]
public sealed class UnitStatusHistory
{
    [Key, Column("IdUnitStatusHistory")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUnitStatusHistory { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    public int PreviousStatusId { get; set; }
    public int NewStatusId { get; set; }

    [Column(TypeName = "date")]
    public DateTime EffectiveDate { get; set; }

    public int? LinkedPersonId { get; set; }

    [Required, MaxLength(300)]
    public string Reason { get; set; } = string.Empty;

    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
}
