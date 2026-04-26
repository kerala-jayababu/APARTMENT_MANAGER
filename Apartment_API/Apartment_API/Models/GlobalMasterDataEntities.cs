using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("AmenityTypes", Schema = "dbo")]
public sealed class AmenityType
{
    [Key]
    [Column("IdAmenityType")]
    public int IdAmenityType { get; set; }

    [Required, MaxLength(50)]
    public string AmenityTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string AmenityTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

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

[Table("BookingChargeTypes", Schema = "dbo")]
public sealed class BookingChargeType
{
    [Key]
    [Column("IdBookingChargeType")]
    public int IdBookingChargeType { get; set; }

    [Required, MaxLength(30)]
    public string ChargeTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ChargeTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("CommitteeMemberStatuses", Schema = "dbo")]
public sealed class CommitteeMemberStatus
{
    [Key]
    [Column("IdCommitteeMemberStatus")]
    public int IdCommitteeMemberStatus { get; set; }

    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("CommitteeRoles", Schema = "dbo")]
public sealed class CommitteeRole
{
    [Key]
    [Column("IdCommitteeRole")]
    public int IdCommitteeRole { get; set; }

    [Required, MaxLength(50)]
    public string RoleCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("ComplaintCategories", Schema = "dbo")]
public sealed class ComplaintCategory
{
    [Key]
    [Column("IdComplaintCategory")]
    public int IdComplaintCategory { get; set; }

    [Required, MaxLength(50)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string CategoryName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

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

[Table("DocumentCategories", Schema = "dbo")]
public sealed class DocumentCategory
{
    [Key]
    [Column("IdDocumentCategory")]
    public int IdDocumentCategory { get; set; }

    [Required, MaxLength(50)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string CategoryName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("ExpenseStatuses", Schema = "dbo")]
public sealed class ExpenseStatus
{
    [Key]
    [Column("IdExpenseStatus")]
    public int IdExpenseStatus { get; set; }

    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("IdentityDocTypes", Schema = "dbo")]
public sealed class IdentityDocType
{
    [Key]
    [Column("IdIdentityDocType")]
    public int IdIdentityDocType { get; set; }

    [Required, MaxLength(30)]
    public string DocTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DocTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("InvoiceStatuses", Schema = "dbo")]
public sealed class InvoiceStatus
{
    [Key]
    [Column("IdInvoiceStatus")]
    public int IdInvoiceStatus { get; set; }

    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("NoticeCategories", Schema = "dbo")]
public sealed class NoticeCategory
{
    [Key]
    [Column("IdNoticeCategory")]
    public int IdNoticeCategory { get; set; }

    [Required, MaxLength(30)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("OwnershipTypes", Schema = "dbo")]
public sealed class OwnershipType
{
    [Key]
    [Column("IdOwnershipType")]
    public int IdOwnershipType { get; set; }

    [Required, MaxLength(30)]
    public string OwnershipCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string OwnershipName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("PaymentModes", Schema = "dbo")]
public sealed class PaymentMode
{
    [Key]
    [Column("IdPaymentMode")]
    public int IdPaymentMode { get; set; }

    [Required, MaxLength(30)]
    public string PaymentModeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PaymentModeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("PersonTypes", Schema = "dbo")]
public sealed class PersonType
{
    [Key]
    [Column("IdPersonType")]
    public int IdPersonType { get; set; }

    [Required, MaxLength(30)]
    public string PersonTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PersonTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("PriorityLevels", Schema = "dbo")]
public sealed class PriorityLevel
{
    [Key]
    [Column("IdPriorityLevel")]
    public int IdPriorityLevel { get; set; }

    [Required, MaxLength(20)]
    public string PriorityCode { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string PriorityName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("ReconciliationStatuses", Schema = "dbo")]
public sealed class ReconciliationStatus
{
    [Key]
    [Column("IdReconciliationStatus")]
    public int IdReconciliationStatus { get; set; }

    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("Roles", Schema = "dbo")]
public sealed class AppRole
{
    [Key]
    [Column("IdRole")]
    public int IdRole { get; set; }

    [Required, MaxLength(50)]
    public string RoleCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("UnitStatuses", Schema = "dbo")]
public sealed class UnitStatus
{
    [Key]
    [Column("IdUnitStatus")]
    public int IdUnitStatus { get; set; }

    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("UnitTypes", Schema = "dbo")]
public sealed class UnitType
{
    [Key]
    [Column("IdUnitType")]
    public int IdUnitType { get; set; }

    [Required, MaxLength(30)]
    public string UnitTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string UnitTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("UtilityTypes", Schema = "dbo")]
public sealed class UtilityType
{
    [Key]
    [Column("IdUtilityType")]
    public int IdUtilityType { get; set; }

    [Required, MaxLength(30)]
    public string UtilityTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string UtilityTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}

[Table("VendorTypes", Schema = "dbo")]
public sealed class VendorType
{
    [Key]
    [Column("IdVendorType")]
    public int IdVendorType { get; set; }

    [Required, MaxLength(50)]
    public string VendorTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string VendorTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
