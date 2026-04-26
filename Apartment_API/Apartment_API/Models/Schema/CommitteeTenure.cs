using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("CommitteeTenures", Schema = "dbo")]
public sealed class CommitteeTenure
{
    [Key, Column("IdCommitteeTenure")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCommitteeTenure { get; set; }

    public int ApartmentId { get; set; }

    [Column(TypeName = "date")]
    public DateTime TenureStartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime TenureEndDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
