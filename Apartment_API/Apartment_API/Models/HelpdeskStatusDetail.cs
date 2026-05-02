using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("HelpdeskStatusDetails", Schema = "dbo")]
public sealed class HelpdeskStatusDetail
{
    [Key, Column("IdHelpDeskStatusDetails")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdHelpDeskStatusDetails { get; set; }

    [Column("IdHelpDesk")]
    public int IdHelpDesk { get; set; }

    public DateTime StatusEntryDate { get; set; }

    public int StatusUpdatedBy { get; set; }

    [Required, MaxLength(1000)]
    public string StatusDetails { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AttachmentDocUrl { get; set; }

    [Required, MaxLength(50)]
    public string HelpdeskStatus { get; set; } = string.Empty;
}
