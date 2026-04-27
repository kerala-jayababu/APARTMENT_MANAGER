using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("MMCBatchLines", Schema = "dbo")]
public sealed class MmcBatchLine
{
    [Key, Column("IdMMCBatchLine")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdMmcBatchLine { get; set; }

    public int MmcBatchId { get; set; }
    public int UnitId { get; set; }

    [Precision(14, 2)]
    public decimal PreviousMmcAmount { get; set; }

    [Precision(14, 2)]
    public decimal NewMmcAmount { get; set; }
}
