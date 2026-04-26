using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("ComplaintStatuses", Schema = "dbo")]
public sealed class ComplaintStatus
{
    [Key]
    [Column("IdComplaintStatus")]
    public int IdComplaintStatus { get; set; }

    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
