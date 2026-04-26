using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("ComplaintUpdates", Schema = "dbo")]
public sealed class ComplaintUpdate
{
    [Key, Column("IdComplaintUpdate")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdComplaintUpdate { get; set; }

    public int ApartmentId { get; set; }
    public int ComplaintId { get; set; }
    public int UpdatedByUserId { get; set; }
    public int? PreviousStatusId { get; set; }
    public int? NewStatusId { get; set; }

    [MaxLength(1000)]
    public string Remarks { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
