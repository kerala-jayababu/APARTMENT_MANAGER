using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("UtilityBillBatchLines", Schema = "dbo")]
public sealed class UtilityBillBatchLine
{
    [Key, Column("IdUtilityBillBatchLine")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUtilityBillBatchLine { get; set; }

    public int ApartmentId { get; set; }
    public int UtilityBillBatchId { get; set; }
    public int UnitId { get; set; }

    [Precision(12, 2)]
    public decimal Amount { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
