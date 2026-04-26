using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("BankAccountTypes", Schema = "dbo")]
public sealed class BankAccountType
{
    [Key]
    [Column("IdBankAccountType")]
    public int IdBankAccountType { get; set; }

    [Required, MaxLength(30)]
    public string AccountTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string AccountTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
