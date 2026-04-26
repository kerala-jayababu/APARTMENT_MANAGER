using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("CommitteeTenureExtensionLog", Schema = "dbo")]
public sealed class CommitteeTenureExtensionLog
{
    [Key, Column("IdCommitteeTenureExtensionLog")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCommitteeTenureExtensionLog { get; set; }

    public int ApartmentId { get; set; }
    public int CommitteeTenureId { get; set; }

    [Column(TypeName = "date")]
    public DateTime PreviousEndDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime NewEndDate { get; set; }

    [Required, MaxLength(500)]
    public string ExtensionReason { get; set; } = string.Empty;

    public int ExtendedByUserId { get; set; }
    public DateTime ExtendedAt { get; set; }
}
