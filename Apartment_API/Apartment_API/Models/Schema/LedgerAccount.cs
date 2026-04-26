using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("LedgerAccounts", Schema = "dbo")]
public sealed class LedgerAccount
{
    [Key, Column("IDLedgerAccount")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdLedgerAccount { get; set; }

    public int ApartmentId { get; set; }
    public int GroupId { get; set; }
    public int? ParentLedgerId { get; set; }

    [Required, MaxLength(30)]
    public string AccountCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string AccountName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ShortName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsControl { get; set; }
    public int? SubLedgerTypeId { get; set; }
    public bool IsPosting { get; set; }
    public bool IsBankAccount { get; set; }
    public bool IsCashAccount { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }

    [Precision(18, 2)]
    public decimal OpeningBalance { get; set; }

    [MaxLength(1)]
    [MinLength(1)]
    public string OpeningBalanceSide { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime? OpeningBalanceAsOf { get; set; }

    public DateTime CreatedOn { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedByUserId { get; set; }
}
