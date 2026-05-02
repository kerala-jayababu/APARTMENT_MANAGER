using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("HelpDesk", Schema = "dbo")]
public sealed class HelpDesk
{
    [Key, Column("IdHelpDesk")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdHelpDesk { get; set; }

    public int ApartmentId { get; set; }

    [Column("OwnerTenantID")]
    public int OwnerTenantId { get; set; }

    public DateTime EntryDate { get; set; }

    [Column("HelpdeskCategoryID")]
    public int HelpdeskCategoryId { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Column("UnitID")]
    public int UnitId { get; set; }

    [Required, MaxLength(50)]
    public string Priority { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AgreementDocUrl { get; set; }

    [Required, MaxLength(50)]
    public string CurrentStatus { get; set; } = string.Empty;
}
