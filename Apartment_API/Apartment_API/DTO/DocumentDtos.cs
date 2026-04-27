namespace Apartment_API.DTO;

public sealed class DocumentListDto
{
    public int Id { get; init; }
    public string DocumentName { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? LinkedEntityType { get; init; }
    public int? LinkedEntityId { get; init; }
    public string LinkedEntityLabel { get; init; } = string.Empty;
    public int? FileSizeKb { get; init; }
    public string? MimeType { get; init; }
    public int UploadedByUserId { get; init; }
    public string UploadedByName { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string ExpiryStatus { get; init; } = "OK";
    public int? DaysToExpiry { get; init; }
}

public sealed class DocumentDetailDto
{
    public int Id { get; init; }
    public string DocumentName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? LinkedEntityType { get; init; }
    public int? LinkedEntityId { get; init; }
    public string LinkedEntityLabel { get; init; } = string.Empty;
    public string? FileUrl { get; init; }
    public int? FileSizeKb { get; init; }
    public string? MimeType { get; init; }
    public int UploadedByUserId { get; init; }
    public string UploadedByName { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public bool IsActive { get; init; }
}

public sealed class UploadDocumentRequest
{
    public string DocumentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string LinkedEntityType { get; set; } = "COMPLEX";
    public int? LinkedEntityId { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public sealed class UpdateDocumentRequest
{
    public string? DocumentName { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string? LinkedEntityType { get; set; }
    public int? LinkedEntityId { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public sealed class DocumentCategoryDto
{
    public int Id { get; init; }
    public string CategoryCode { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
}

public sealed class LinkedEntityDto
{
    public int? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? SubLabel { get; init; }
}

public sealed class ExpiringDocumentDto
{
    public int Id { get; init; }
    public string DocumentName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string LinkedEntityLabel { get; init; } = string.Empty;
    public DateTime ExpiryDate { get; init; }
    public int DaysToExpiry { get; init; }
    public string ExpiryStatus { get; init; } = "OK";
}

public sealed class CategoryCountDto
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class DocumentStatsDto
{
    public int TotalActive { get; init; }
    public int ExpiringIn7Days { get; init; }
    public int ExpiringIn30Days { get; init; }
    public int ExpiringIn60Days { get; init; }
    public int ExpiredNotRenewed { get; init; }
    public int UploadedThisMonth { get; init; }
    public IReadOnlyList<CategoryCountDto> ByCategory { get; init; } = [];
}

public sealed class DocumentPreviewUrlDto
{
    public string Url { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string? MimeType { get; init; }
}
