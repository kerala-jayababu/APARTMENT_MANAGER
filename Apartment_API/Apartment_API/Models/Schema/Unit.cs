using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Units", Schema = "dbo")]
public sealed class Unit
{
    [Key, Column("IdUnit")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUnit { get; set; }

    public int ApartmentId { get; set; }

    /// <summary>FK → Blocks.IdBlock (nullable until backfilled from legacy <see cref="Block" /> text).</summary>
    public int? BlockId { get; set; }

    [Required, MaxLength(20)]
    public string UnitNumber { get; set; } = string.Empty;

    /// <summary>Legacy display/code string; prefer <see cref="BlockId"/> when set.</summary>
    [MaxLength(20)]
    public string? Block { get; set; }

    public short Floor { get; set; }
    public int UnitTypeId { get; set; }

    [Precision(10, 2)]
    public decimal? BuiltUpArea { get; set; }

    [Precision(10, 2)]
    public decimal? CarpetArea { get; set; }

    [MaxLength(20)]
    public string? Facing { get; set; }

    public int UnitStatusId { get; set; }
    public int OwnershipTypeId { get; set; }
    public int? IdCurrentOwner { get; set; }

    /// <summary>SQL column is <c>CurrentMMCAmount</c>.</summary>
    [Column("CurrentMMCAmount")]
    [Precision(12, 2)]
    public decimal CurrentMmcAmount { get; set; }

    [Column("ControlLedgerAccountID")]
    public int? ControlLedgerAccountId { get; set; }

    [Precision(18, 2)]
    public decimal OpeningReceivable { get; set; }

    [Column(TypeName = "date")]
    public DateTime? OpeningReceivableAsOn { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
