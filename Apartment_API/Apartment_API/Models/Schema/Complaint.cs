using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Complaints", Schema = "dbo")]
public sealed class Complaint
{
    [Key, Column("IdComplaint")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdComplaint { get; set; }

    public int ApartmentId { get; set; }

    [MaxLength(20)]
    public string? TicketNumber { get; set; }

    public int UnitId { get; set; }
    public int RaisedByPersonId { get; set; }
    public int CategoryId { get; set; }
    public int PriorityId { get; set; }

    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    public int? AssignedToUserId { get; set; }
    public int StatusId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    [MaxLength(1000)]
    public string? ResolutionNotes { get; set; }

    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
