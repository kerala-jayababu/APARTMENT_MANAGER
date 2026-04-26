using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("CommitteeMembers", Schema = "dbo")]
public sealed class CommitteeMember
{
    [Key, Column("IdCommitteeMember")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCommitteeMember { get; set; }

    public int ApartmentId { get; set; }
    public int CommitteeTenureId { get; set; }
    public int PersonId { get; set; }
    public int CommitteeRoleId { get; set; }

    [Column(TypeName = "date")]
    public DateTime EffectiveFromDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EffectiveToDate { get; set; }

    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
