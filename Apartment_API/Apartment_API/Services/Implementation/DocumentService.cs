using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class DocumentService(AppDbContext db, IWebHostEnvironment env) : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };

    private const long MaxFileSize = 10 * 1024 * 1024;
    private const string Complex = "COMPLEX";
    private const string Unit = "UNIT";
    private const string Owner = "OWNER";
    private const string Tenant = "TENANT";
    private const string Vendor = "VENDOR";

    public async Task<PagedResult<DocumentListDto>> ListAsync(
        int apartmentId,
        string? search,
        int? categoryId,
        string? linkedEntityType,
        int? linkedEntityId,
        int? uploadedByUserId,
        DateTime? uploadedFrom,
        DateTime? uploadedTo,
        int? expiringWithinDays,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDir,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var today = DateTime.UtcNow.Date;

        var q = db.StoredDocuments.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.DocumentName.Contains(s) || (x.Description != null && x.Description.Contains(s)));
        }

        if (categoryId is { } catId) q = q.Where(x => x.CategoryId == catId);
        if (uploadedByUserId is { } uid) q = q.Where(x => x.UploadedByUserId == uid);
        if (uploadedFrom is { } uf) q = q.Where(x => x.CreatedAt >= uf.Date);
        if (uploadedTo is { } ut) q = q.Where(x => x.CreatedAt < ut.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(linkedEntityType))
        {
            var t = NormalizeLinkedType(linkedEntityType);
            q = q.Where(x => x.LinkedEntityType == t);
            if (linkedEntityId is { } lid) q = q.Where(x => x.LinkedEntityId == lid);
        }
        else if (linkedEntityId is { } onlyLinkedId)
        {
            q = q.Where(x => x.LinkedEntityId == onlyLinkedId);
        }

        if (expiringWithinDays is { } n && n >= 0)
        {
            var until = today.AddDays(n);
            q = q.Where(x => x.ExpiryDate != null && x.ExpiryDate <= until);
        }

        q = ApplySort(q, sortBy, sortDir);

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
            return new PagedResult<DocumentListDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };

        var catIds = rows.Select(x => x.CategoryId).Distinct().ToList();
        var userIds = rows.Select(x => x.UploadedByUserId).Distinct().ToList();
        var categories = await db.DocumentCategories.AsNoTracking()
            .Where(x => catIds.Contains(x.IdDocumentCategory))
            .ToDictionaryAsync(x => x.IdDocumentCategory, x => x.CategoryName, cancellationToken)
            .ConfigureAwait(false);
        var users = await db.Users.AsNoTracking()
            .Where(x => userIds.Contains(x.IdUser))
            .ToDictionaryAsync(x => x.IdUser, x => x.FullName, cancellationToken)
            .ConfigureAwait(false);

        var linkedLabels = await BuildLinkedLabelsAsync(apartmentId, rows, cancellationToken).ConfigureAwait(false);

        var items = rows.Select(x =>
        {
            var (status, days) = ComputeExpiryStatus(x.ExpiryDate, today);
            return new DocumentListDto
            {
                Id = x.IdDocument,
                DocumentName = x.DocumentName,
                CategoryId = x.CategoryId,
                CategoryName = categories.GetValueOrDefault(x.CategoryId) ?? string.Empty,
                LinkedEntityType = x.LinkedEntityType,
                LinkedEntityId = x.LinkedEntityId,
                LinkedEntityLabel = linkedLabels.GetValueOrDefault(x.IdDocument) ?? string.Empty,
                FileSizeKb = x.FileSizeKb,
                MimeType = x.MimeType,
                UploadedByUserId = x.UploadedByUserId,
                UploadedByName = users.GetValueOrDefault(x.UploadedByUserId) ?? string.Empty,
                UploadedAt = x.CreatedAt,
                ExpiryDate = x.ExpiryDate,
                ExpiryStatus = status,
                DaysToExpiry = days
            };
        }).ToList();

        return new PagedResult<DocumentListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<DocumentDetailDto?> GetAsync(int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var row = await db.StoredDocuments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdDocument == id, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) return null;

        var catName = await db.DocumentCategories.AsNoTracking()
            .Where(x => x.IdDocumentCategory == row.CategoryId)
            .Select(x => x.CategoryName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;
        var uploadedByName = await db.Users.AsNoTracking()
            .Where(x => x.IdUser == row.UploadedByUserId)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;
        var labels = await BuildLinkedLabelsAsync(apartmentId, [row], cancellationToken).ConfigureAwait(false);

        return new DocumentDetailDto
        {
            Id = row.IdDocument,
            DocumentName = row.DocumentName,
            Description = row.Description,
            CategoryId = row.CategoryId,
            CategoryName = catName,
            LinkedEntityType = row.LinkedEntityType,
            LinkedEntityId = row.LinkedEntityId,
            LinkedEntityLabel = labels.GetValueOrDefault(row.IdDocument) ?? string.Empty,
            FileUrl = row.FileUrl,
            FileSizeKb = row.FileSizeKb,
            MimeType = row.MimeType,
            UploadedByUserId = row.UploadedByUserId,
            UploadedByName = uploadedByName,
            UploadedAt = row.CreatedAt,
            ExpiryDate = row.ExpiryDate,
            IsActive = row.IsActive
        };
    }

    public async Task<int> UploadAsync(
        int apartmentId,
        int userId,
        UploadDocumentRequest request,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateUploadRequest(request);
        ValidateFile(file);
        await ValidateCategoryAndLinkedEntityAsync(apartmentId, request.CategoryId, request.LinkedEntityType, request.LinkedEntityId, cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var normalizedType = NormalizeLinkedType(request.LinkedEntityType);
        var normalizedName = request.DocumentName.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var ext = Path.GetExtension(file.FileName);
        var key = BuildStorageKey(apartmentId, now, normalizedName, ext);
        var fullPath = ResolvePhysicalPathFromKey(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = File.Create(fullPath))
        {
            await file.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }

        var row = new StoredDocument
        {
            ApartmentId = apartmentId,
            CategoryId = request.CategoryId,
            DocumentName = normalizedName,
            Description = normalizedDescription,
            FileUrl = key,
            FileSizeKb = (int)Math.Ceiling(file.Length / 1024d),
            MimeType = string.IsNullOrWhiteSpace(file.ContentType) ? GuessMimeFromExtension(ext) : file.ContentType,
            LinkedEntityType = normalizedType,
            LinkedEntityId = normalizedType == Complex ? null : request.LinkedEntityId,
            UploadedByUserId = userId,
            ExpiryDate = request.ExpiryDate?.Date,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        db.StoredDocuments.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return row.IdDocument;
    }

    public async Task UpdateAsync(
        int apartmentId,
        int userId,
        int id,
        UpdateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = await db.StoredDocuments
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdDocument == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) throw new InvalidOperationException("Document not found.");
        if (row.UploadedByUserId != userId) throw new UnauthorizedAccessException("Only the uploader can update this document.");

        if (request.DocumentName != null)
        {
            var value = request.DocumentName.Trim();
            if (value.Length == 0 || value.Length > 150) throw new InvalidOperationException("DocumentName must be 1..150 characters.");
            row.DocumentName = value;
        }
        if (request.Description != null)
        {
            var v = request.Description.Trim();
            if (v.Length > 300) throw new InvalidOperationException("Description can be at most 300 characters.");
            row.Description = v;
        }
        if (request.CategoryId is { } catId)
        {
            var catOk = await db.DocumentCategories.AsNoTracking()
                .AnyAsync(x => x.IdDocumentCategory == catId && x.IsActive, cancellationToken)
                .ConfigureAwait(false);
            if (!catOk) throw new InvalidOperationException("Invalid CategoryId.");
            row.CategoryId = catId;
        }
        var nextType = row.LinkedEntityType ?? Complex;
        var nextLinkedId = row.LinkedEntityId;
        if (request.LinkedEntityType != null) nextType = NormalizeLinkedType(request.LinkedEntityType);
        if (request.LinkedEntityId is { } lid || request.LinkedEntityType != null)
            nextLinkedId = request.LinkedEntityType != null && NormalizeLinkedType(request.LinkedEntityType) == Complex ? null : request.LinkedEntityId;
        await ValidateLinkedEntityOnlyAsync(apartmentId, nextType, nextLinkedId, cancellationToken).ConfigureAwait(false);
        row.LinkedEntityType = nextType;
        row.LinkedEntityId = nextType == Complex ? null : nextLinkedId;

        if (request.ExpiryDate is { } ex)
        {
            if (ex.Date < DateTime.UtcNow.Date) throw new InvalidOperationException("ExpiryDate cannot be in the past.");
            row.ExpiryDate = ex.Date;
        }

        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(Stream Stream, string ContentType, string FileName, long? Length)> DownloadAsync(
        int apartmentId,
        int id,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var row = await db.StoredDocuments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdDocument == id && x.IsActive)
            .ConfigureAwait(false);
        if (row is null) throw new InvalidOperationException("Document not found.");
        var fullPath = ResolvePhysicalPath(row.FileUrl);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Document file not found.");
        var fs = File.OpenRead(fullPath);
        var ext = Path.GetExtension(fullPath);
        var fileName = $"{SanitizeFileName(row.DocumentName)}{ext}";
        return (fs, row.MimeType ?? GuessMimeFromExtension(ext), fileName, fs.Length);
    }

    public async Task<DocumentPreviewUrlDto> GetPreviewUrlAsync(int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var row = await db.StoredDocuments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdDocument == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Document not found.");
        var key = row.FileUrl ?? string.Empty;
        var url = key.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ? key : $"/uploads/{key.TrimStart('/')}";
        return new DocumentPreviewUrlDto
        {
            Url = url.Replace("\\", "/"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            MimeType = row.MimeType
        };
    }

    public async Task DeleteAsync(int apartmentId, int userId, int id, CancellationToken cancellationToken = default)
    {
        var row = await db.StoredDocuments
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdDocument == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) throw new InvalidOperationException("Document not found.");
        if (row.UploadedByUserId != userId) throw new UnauthorizedAccessException("Only the uploader can delete this document.");
        row.IsActive = false;
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DocumentCategoryDto>> ListCategoriesAsync(bool isActive, CancellationToken cancellationToken = default)
    {
        var q = db.DocumentCategories.AsNoTracking();
        if (isActive) q = q.Where(x => x.IsActive);
        return await q.OrderBy(x => x.SortOrder).ThenBy(x => x.CategoryName)
            .Select(x => new DocumentCategoryDto
            {
                Id = x.IdDocumentCategory,
                CategoryCode = x.CategoryCode,
                CategoryName = x.CategoryName,
                SortOrder = x.SortOrder
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LinkedEntityDto>> SearchLinkedEntitiesAsync(
        int apartmentId,
        string type,
        string? search,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 50);
        var normalized = NormalizeLinkedType(type);
        var s = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (s != null && s.Length < 2 && normalized != Complex) return [];

        if (normalized == Complex)
        {
            var apt = await db.Apartments.AsNoTracking()
                .Where(x => x.IdApartment == apartmentId)
                .Select(x => new LinkedEntityDto { Id = null, Label = x.ApartmentName, SubLabel = "Complex" })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return apt is null ? [] : [apt];
        }

        if (normalized == Unit)
        {
            var q = db.Units.AsNoTracking().Where(x => x.ApartmentId == apartmentId && x.IsActive);
            if (s != null) q = q.Where(x => x.UnitNumber.Contains(s) || (x.Block != null && x.Block.Contains(s)));
            return await q.OrderBy(x => x.UnitNumber).Take(take)
                .Select(x => new LinkedEntityDto { Id = x.IdUnit, Label = x.UnitNumber, SubLabel = x.Block })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        if (normalized == Owner)
        {
            var q = from uo in db.UnitOwners.AsNoTracking()
                    where uo.ApartmentId == apartmentId && uo.IsActive
                    join p in db.Persons.AsNoTracking() on uo.PersonId equals p.IdPerson
                    join u in db.Units.AsNoTracking() on uo.UnitId equals u.IdUnit
                    where p.ApartmentId == apartmentId && p.IsActive && u.ApartmentId == apartmentId && u.IsActive
                    select new { p.IdPerson, p.FullName, u.UnitNumber };
            if (s != null) q = q.Where(x => x.FullName.Contains(s) || x.UnitNumber.Contains(s));
            return await q.OrderBy(x => x.FullName).Take(take)
                .Select(x => new LinkedEntityDto { Id = x.IdPerson, Label = x.FullName, SubLabel = x.UnitNumber })
                .Distinct()
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        if (normalized == Tenant)
        {
            var q = from t in db.TenantAssignments.AsNoTracking()
                    where t.ApartmentId == apartmentId && t.IsActive
                    join p in db.Persons.AsNoTracking() on t.PersonId equals p.IdPerson
                    join u in db.Units.AsNoTracking() on t.UnitId equals u.IdUnit
                    where p.ApartmentId == apartmentId && p.IsActive && u.ApartmentId == apartmentId && u.IsActive
                    select new { p.IdPerson, p.FullName, u.UnitNumber };
            if (s != null) q = q.Where(x => x.FullName.Contains(s) || x.UnitNumber.Contains(s));
            return await q.OrderBy(x => x.FullName).Take(take)
                .Select(x => new LinkedEntityDto { Id = x.IdPerson, Label = x.FullName, SubLabel = x.UnitNumber })
                .Distinct()
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        var vq = db.Vendors.AsNoTracking().Where(x => x.ApartmentId == apartmentId && x.IsActive);
        if (s != null) vq = vq.Where(x => x.VendorName.Contains(s) || (x.ContactPerson != null && x.ContactPerson.Contains(s)));
        return await vq.OrderBy(x => x.VendorName).Take(take)
            .Select(x => new LinkedEntityDto { Id = x.IdVendor, Label = x.VendorName, SubLabel = x.ContactPerson })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExpiringDocumentDto>> GetExpiringAsync(
        int apartmentId,
        int windowDays,
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        windowDays = Math.Clamp(windowDays, 1, 365);
        var today = DateTime.UtcNow.Date;
        var until = today.AddDays(windowDays);
        var q = db.StoredDocuments.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IsActive && x.ExpiryDate != null && x.ExpiryDate <= until);
        if (categoryId is { } catId) q = q.Where(x => x.CategoryId == catId);
        var rows = await q.OrderBy(x => x.ExpiryDate).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0) return [];
        var categories = await db.DocumentCategories.AsNoTracking()
            .ToDictionaryAsync(x => x.IdDocumentCategory, x => x.CategoryName, cancellationToken).ConfigureAwait(false);
        var linked = await BuildLinkedLabelsAsync(apartmentId, rows, cancellationToken).ConfigureAwait(false);
        return rows.Select(x =>
        {
            var (_, days) = ComputeExpiryStatus(x.ExpiryDate, today);
            return new ExpiringDocumentDto
            {
                Id = x.IdDocument,
                DocumentName = x.DocumentName,
                CategoryName = categories.GetValueOrDefault(x.CategoryId) ?? string.Empty,
                LinkedEntityLabel = linked.GetValueOrDefault(x.IdDocument) ?? string.Empty,
                ExpiryDate = x.ExpiryDate!.Value.Date,
                DaysToExpiry = days ?? 0,
                ExpiryStatus = ComputeExpiryStatus(x.ExpiryDate, today).Status
            };
        }).OrderBy(x => x.DaysToExpiry).ToList();
    }

    public async Task<DocumentStatsDto> GetStatsAsync(int apartmentId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var q = db.StoredDocuments.AsNoTracking().Where(x => x.ApartmentId == apartmentId && x.IsActive);
        var rows = await q.Select(x => new { x.CategoryId, x.ExpiryDate, x.CreatedAt }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var categories = await db.DocumentCategories.AsNoTracking().ToDictionaryAsync(x => x.IdDocumentCategory, x => x.CategoryName, cancellationToken).ConfigureAwait(false);
        var byCategory = rows.GroupBy(x => x.CategoryId)
            .Select(g => new CategoryCountDto
            {
                CategoryId = g.Key,
                CategoryName = categories.GetValueOrDefault(g.Key) ?? string.Empty,
                Count = g.Count()
            }).OrderBy(x => x.CategoryName).ToList();

        return new DocumentStatsDto
        {
            TotalActive = rows.Count,
            ExpiringIn7Days = rows.Count(x => x.ExpiryDate is { } d && d >= today && d <= today.AddDays(7)),
            ExpiringIn30Days = rows.Count(x => x.ExpiryDate is { } d && d >= today && d <= today.AddDays(30)),
            ExpiringIn60Days = rows.Count(x => x.ExpiryDate is { } d && d >= today && d <= today.AddDays(60)),
            ExpiredNotRenewed = rows.Count(x => x.ExpiryDate is { } d && d < today),
            UploadedThisMonth = rows.Count(x => x.CreatedAt >= monthStart),
            ByCategory = byCategory
        };
    }

    private static IQueryable<StoredDocument> ApplySort(IQueryable<StoredDocument> q, string? sortBy, string? sortDir)
    {
        var desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var key = (sortBy ?? "uploadedAt").Trim().ToLowerInvariant();
        return key switch
        {
            "documentname" => desc ? q.OrderByDescending(x => x.DocumentName) : q.OrderBy(x => x.DocumentName),
            "expirydate" => desc ? q.OrderByDescending(x => x.ExpiryDate) : q.OrderBy(x => x.ExpiryDate),
            "categoryname" => desc ? q.OrderByDescending(x => x.CategoryId) : q.OrderBy(x => x.CategoryId),
            _ => desc ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt)
        };
    }

    private static (string Status, int? Days) ComputeExpiryStatus(DateTime? expiryDate, DateTime today)
    {
        if (expiryDate is not { } d) return ("OK", null);
        var days = (d.Date - today).Days;
        if (days < 0) return ("EXPIRED", days);
        if (days <= 7) return ("CRITICAL", days);
        if (days <= 30) return ("WARNING", days);
        if (days <= 60) return ("INFO", days);
        return ("OK", days);
    }

    private static void ValidateUploadRequest(UploadDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentName)) throw new InvalidOperationException("DocumentName is required.");
        if (request.DocumentName.Trim().Length > 150) throw new InvalidOperationException("DocumentName can be at most 150 characters.");
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 300)
            throw new InvalidOperationException("Description can be at most 300 characters.");
        if (request.CategoryId <= 0) throw new InvalidOperationException("CategoryId is required.");
        if (request.ExpiryDate is { } ex && ex.Date < DateTime.UtcNow.Date) throw new InvalidOperationException("ExpiryDate cannot be in the past.");
        _ = NormalizeLinkedType(request.LinkedEntityType);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length <= 0) throw new InvalidOperationException("file is required.");
        if (file.Length > MaxFileSize) throw new InvalidOperationException("File size cannot exceed 10 MB.");
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Unsupported file type.");
    }

    private async Task ValidateCategoryAndLinkedEntityAsync(
        int apartmentId,
        int categoryId,
        string linkedEntityType,
        int? linkedEntityId,
        CancellationToken cancellationToken)
    {
        var catOk = await db.DocumentCategories.AsNoTracking()
            .AnyAsync(x => x.IdDocumentCategory == categoryId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (!catOk) throw new InvalidOperationException("Invalid CategoryId.");
        await ValidateLinkedEntityOnlyAsync(apartmentId, NormalizeLinkedType(linkedEntityType), linkedEntityId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateLinkedEntityOnlyAsync(
        int apartmentId,
        string linkedEntityType,
        int? linkedEntityId,
        CancellationToken cancellationToken)
    {
        if (linkedEntityType == Complex) return;
        if (linkedEntityId is not { } id) throw new InvalidOperationException("linkedEntityId is required.");
        var exists = linkedEntityType switch
        {
            Unit => await db.Units.AsNoTracking().AnyAsync(x => x.ApartmentId == apartmentId && x.IdUnit == id && x.IsActive, cancellationToken).ConfigureAwait(false),
            Owner => await db.Persons.AsNoTracking().AnyAsync(x => x.ApartmentId == apartmentId && x.IdPerson == id && x.IsActive, cancellationToken).ConfigureAwait(false),
            Tenant => await db.Persons.AsNoTracking().AnyAsync(x => x.ApartmentId == apartmentId && x.IdPerson == id && x.IsActive, cancellationToken).ConfigureAwait(false),
            Vendor => await db.Vendors.AsNoTracking().AnyAsync(x => x.ApartmentId == apartmentId && x.IdVendor == id && x.IsActive, cancellationToken).ConfigureAwait(false),
            _ => false
        };
        if (!exists) throw new InvalidOperationException("Invalid linked entity.");
    }

    private static string NormalizeLinkedType(string? type)
    {
        var t = (type ?? string.Empty).Trim().ToUpperInvariant();
        return t switch
        {
            Complex or Unit or Owner or Tenant or Vendor => t,
            _ => throw new InvalidOperationException("linkedEntityType must be COMPLEX, UNIT, OWNER, TENANT, or VENDOR.")
        };
    }

    private string ResolvePhysicalPath(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) throw new InvalidOperationException("Document has no file key.");
        var value = fileUrl.Replace("\\", "/");
        if (value.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var rel = value["/uploads/".Length..].TrimStart('/');
            return Path.Combine(env.ContentRootPath, "Uploads", rel.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
        return ResolvePhysicalPathFromKey(value);
    }

    private string ResolvePhysicalPathFromKey(string key)
    {
        var rel = key.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
        return Path.Combine(env.ContentRootPath, "Uploads", rel);
    }

    private static string BuildStorageKey(int apartmentId, DateTime nowUtc, string documentName, string extension)
    {
        var slug = SanitizeFileName(documentName);
        return $"documents/{nowUtc:yyyy}/{nowUtc:MM}/{apartmentId}-{Guid.NewGuid():N}-{slug}{extension}".ToLowerInvariant();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Trim().Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        clean = string.Join("-", clean.Split([' ', '_'], StringSplitOptions.RemoveEmptyEntries));
        return clean.Length > 80 ? clean[..80] : clean;
    }

    private static string GuessMimeFromExtension(string? ext) => (ext ?? string.Empty).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };

    private async Task<Dictionary<int, string>> BuildLinkedLabelsAsync(
        int apartmentId,
        IReadOnlyList<StoredDocument> rows,
        CancellationToken cancellationToken)
    {
        var result = rows.ToDictionary(x => x.IdDocument, _ => string.Empty);
        if (rows.Count == 0) return result;

        var complexDocIds = rows.Where(x => string.Equals(x.LinkedEntityType, Complex, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.IdDocument).ToList();
        if (complexDocIds.Count > 0)
        {
            var aptName = await db.Apartments.AsNoTracking()
                .Where(x => x.IdApartment == apartmentId)
                .Select(x => x.ApartmentName)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false) ?? string.Empty;
            foreach (var docId in complexDocIds) result[docId] = aptName;
        }

        var unitIds = rows.Where(x => string.Equals(x.LinkedEntityType, Unit, StringComparison.OrdinalIgnoreCase) && x.LinkedEntityId != null)
            .Select(x => x.LinkedEntityId!.Value).Distinct().ToList();
        if (unitIds.Count > 0)
        {
            var unitMap = await db.Units.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId && unitIds.Contains(x.IdUnit))
                .ToDictionaryAsync(x => x.IdUnit, x => x.UnitNumber, cancellationToken)
                .ConfigureAwait(false);
            foreach (var row in rows.Where(x => string.Equals(x.LinkedEntityType, Unit, StringComparison.OrdinalIgnoreCase) && x.LinkedEntityId != null))
                result[row.IdDocument] = unitMap.GetValueOrDefault(row.LinkedEntityId!.Value) ?? string.Empty;
        }

        var personIds = rows.Where(x =>
                (string.Equals(x.LinkedEntityType, Owner, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(x.LinkedEntityType, Tenant, StringComparison.OrdinalIgnoreCase)) &&
                x.LinkedEntityId != null)
            .Select(x => x.LinkedEntityId!.Value).Distinct().ToList();
        if (personIds.Count > 0)
        {
            var personMap = await db.Persons.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId && personIds.Contains(x.IdPerson))
                .ToDictionaryAsync(x => x.IdPerson, x => x.FullName, cancellationToken)
                .ConfigureAwait(false);
            foreach (var row in rows.Where(x =>
                         (string.Equals(x.LinkedEntityType, Owner, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(x.LinkedEntityType, Tenant, StringComparison.OrdinalIgnoreCase)) &&
                         x.LinkedEntityId != null))
                result[row.IdDocument] = personMap.GetValueOrDefault(row.LinkedEntityId!.Value) ?? string.Empty;
        }

        var vendorIds = rows.Where(x => string.Equals(x.LinkedEntityType, Vendor, StringComparison.OrdinalIgnoreCase) && x.LinkedEntityId != null)
            .Select(x => x.LinkedEntityId!.Value).Distinct().ToList();
        if (vendorIds.Count > 0)
        {
            var vendorMap = await db.Vendors.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId && vendorIds.Contains(x.IdVendor))
                .ToDictionaryAsync(x => x.IdVendor, x => x.VendorName, cancellationToken)
                .ConfigureAwait(false);
            foreach (var row in rows.Where(x => string.Equals(x.LinkedEntityType, Vendor, StringComparison.OrdinalIgnoreCase) && x.LinkedEntityId != null))
                result[row.IdDocument] = vendorMap.GetValueOrDefault(row.LinkedEntityId!.Value) ?? string.Empty;
        }

        return result;
    }
}
