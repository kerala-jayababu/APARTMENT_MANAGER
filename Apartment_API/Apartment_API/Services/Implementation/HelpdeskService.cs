using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class HelpdeskService(AppDbContext db, IWebHostEnvironment env) : IHelpdeskService
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Medium", "High", "Critical"
    };

    private static readonly HashSet<string> OpenWorkflowStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Open", "In progress", "Reopened"
    };

    private static readonly HashSet<string> AdminRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN", "APARTMENT_ADMIN", "SOCIETY_ADMIN", "COMMITTEE_ADMIN"
    };

    private static readonly string StatusOpen = "Open";
    private static readonly string StatusInProgress = "In progress";
    private static readonly string StatusResolved = "Resolved";
    private static readonly string StatusClosed = "Closed";
    private static readonly string StatusReopened = "Reopened";

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = BuildTransitions();

    private static Dictionary<string, HashSet<string>> BuildTransitions()
    {
        var cmp = StringComparer.OrdinalIgnoreCase;
        Dictionary<string, HashSet<string>> map = new(cmp);
        map[StatusOpen] = new HashSet<string>(new[] { StatusInProgress, StatusResolved, StatusClosed }, cmp);
        map[StatusInProgress] = new HashSet<string>(new[] { StatusResolved, StatusClosed }, cmp);
        map[StatusResolved] = new HashSet<string>(new[] { StatusClosed, StatusReopened }, cmp);
        map[StatusClosed] = new HashSet<string>(new[] { StatusReopened }, cmp);
        map[StatusReopened] = new HashSet<string>(new[] { StatusInProgress, StatusResolved }, cmp);
        return map;
    }

    public async Task<bool> IsHelpdeskAdminAsync(int? roleId, CancellationToken cancellationToken = default)
    {
        if (roleId is not { } rid) return false;
        var code = await db.AppRoles.AsNoTracking()
            .Where(r => r.IdRole == rid && r.IsActive)
            .Select(r => r.RoleCode)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return code is not null && AdminRoleCodes.Contains(code.Trim());
    }

    public async Task<HelpDeskCategoryListDto> ListCategoriesAsync(
        int apartmentId, string? q, bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var active = activeOnly ?? true;
        var query = db.HelpDeskCategories.AsNoTracking().Where(x => x.ApartmentId == apartmentId);
        if (active) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(x => x.HelpDeskCategoryName.Contains(s));
        }
        var items = await query.OrderBy(x => x.HelpDeskCategoryName)
            .Select(x => new HelpDeskCategoryDto
            {
                IdHelpDeskCategory = x.IdHelpDeskCategory,
                HelpDeskCategoryName = x.HelpDeskCategoryName
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new HelpDeskCategoryListDto { Items = items, Total = items.Count };
    }

    public async Task<HelpDeskCategoryDto?> GetCategoryAsync(int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        return await db.HelpDeskCategories.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IdHelpDeskCategory == id)
            .Select(x => new HelpDeskCategoryDto
            {
                IdHelpDeskCategory = x.IdHelpDeskCategory,
                HelpDeskCategoryName = x.HelpDeskCategoryName
            })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CreateCategoryAsync(
        int apartmentId, CreateHelpDeskCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.HelpDeskCategoryName ?? string.Empty).Trim();
        if (name.Length is < 1 or > 50) throw new InvalidOperationException("helpDeskCategoryName must be 1..50 characters.");
        var dup = await db.HelpDeskCategories.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.HelpDeskCategoryName.ToLower() == name.ToLower(),
                cancellationToken).ConfigureAwait(false);
        if (dup) throw new InvalidOperationException("CONFLICT: Category name already exists.");
        var row = new HelpDeskCategory
        {
            ApartmentId = apartmentId,
            HelpDeskCategoryName = name,
            IsActive = true
        };
        db.HelpDeskCategories.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return row.IdHelpDeskCategory;
    }

    public async Task UpdateCategoryAsync(
        int apartmentId, int id, UpdateHelpDeskCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.HelpDeskCategoryName ?? string.Empty).Trim();
        if (name.Length is < 1 or > 50) throw new InvalidOperationException("helpDeskCategoryName must be 1..50 characters.");
        var row = await db.HelpDeskCategories
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdHelpDeskCategory == id, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Category not found.");
        var dup = await db.HelpDeskCategories.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.IdHelpDeskCategory != id && x.HelpDeskCategoryName.ToLower() == name.ToLower(),
                cancellationToken).ConfigureAwait(false);
        if (dup) throw new InvalidOperationException("CONFLICT: Category name already exists.");
        row.HelpDeskCategoryName = name;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCategoryAsync(int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var used = await db.HelpDesks.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.HelpdeskCategoryId == id, cancellationToken)
            .ConfigureAwait(false);
        if (used) throw new InvalidOperationException("CONFLICT: Category is referenced by complaints.");
        var row = await db.HelpDeskCategories
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdHelpDeskCategory == id, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Category not found.");
        db.HelpDeskCategories.Remove(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<LogComplaintResponseDto> LogComplaintAsync(
        int apartmentId,
        int userId,
        LogComplaintRequest request,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        ValidateComplaintRequest(request);
        var catOk = await db.HelpDeskCategories.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.IdHelpDeskCategory == request.HelpdeskCategoryID && x.IsActive,
                cancellationToken).ConfigureAwait(false);
        if (!catOk) throw new InvalidOperationException("Invalid helpdeskCategoryID.");

        int unitId;
        int ownerTenantId;
        if (isAdmin && request.UnitID is { } uAdmin && request.OwnerTenantID is { } oAdmin)
        {
            var unitOk = await db.Units.AsNoTracking()
                .AnyAsync(x => x.ApartmentId == apartmentId && x.IdUnit == uAdmin && x.IsActive, cancellationToken)
                .ConfigureAwait(false);
            if (!unitOk) throw new InvalidOperationException("Invalid unitID.");
            var userOk = await db.ApartmentUsers.AsNoTracking()
                .AnyAsync(x => x.ApartmentId == apartmentId && x.UserId == oAdmin && x.IsActive, cancellationToken)
                .ConfigureAwait(false);
            if (!userOk) throw new InvalidOperationException("Invalid ownerTenantID for this apartment.");
            unitId = uAdmin;
            ownerTenantId = oAdmin;
        }
        else
        {
            if (isAdmin && (request.UnitID.HasValue ^ request.OwnerTenantID.HasValue))
                throw new InvalidOperationException("Provide both unitID and ownerTenantID for admin override, or neither.");
            var resolvedUnit = await ResolveCallerUnitAsync(apartmentId, userId, cancellationToken).ConfigureAwait(false);
            if (resolvedUnit is null) throw new InvalidOperationException("No unit context for the current user.");
            unitId = resolvedUnit.Value;
            ownerTenantId = userId;
            if (!await UserHasUnitAccessAsync(apartmentId, userId, unitId, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("You do not have access to this unit.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var hd = new HelpDesk
        {
            ApartmentId = apartmentId,
            OwnerTenantId = ownerTenantId,
            EntryDate = now,
            HelpdeskCategoryId = request.HelpdeskCategoryID,
            Title = request.Title.Trim(),
            UnitId = unitId,
            Priority = NormalizePriority(request.Priority),
            Description = request.Description.Trim(),
            AgreementDocUrl = string.IsNullOrWhiteSpace(request.AgreementDocUrl) ? null : request.AgreementDocUrl.Trim(),
            CurrentStatus = StatusOpen
        };
        db.HelpDesks.Add(hd);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var first = new HelpdeskStatusDetail
        {
            IdHelpDesk = hd.IdHelpDesk,
            StatusEntryDate = now,
            StatusUpdatedBy = userId,
            StatusDetails = "Complaint logged",
            AttachmentDocUrl = null,
            HelpdeskStatus = StatusOpen
        };
        db.HelpdeskStatusDetails.Add(first);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new LogComplaintResponseDto
        {
            IdHelpDesk = hd.IdHelpDesk,
            ComplaintCode = ComplaintCode(hd.IdHelpDesk),
            CurrentStatus = StatusOpen,
            EntryDate = now,
            FirstStatusEntryId = first.IdHelpDeskStatusDetails
        };
    }

    public async Task<PagedResult<ComplaintListItemDto>> ListComplaintsAsync(
        int apartmentId,
        int userId,
        bool isAdmin,
        int? categoryId,
        string? status,
        string? priority,
        int? unitId,
        int? ownerTenantId,
        DateOnly? from,
        DateOnly? to,
        string? q,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.HelpDesks.AsNoTracking().Where(x => x.ApartmentId == apartmentId);
        if (!isAdmin) query = query.Where(x => x.OwnerTenantId == userId);
        if (categoryId is { } c) query = query.Where(x => x.HelpdeskCategoryId == c);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(x => x.Priority.ToLower() == priority.Trim().ToLower());
        if (unitId is { } uid) query = query.Where(x => x.UnitId == uid);
        if (ownerTenantId is { } ot && isAdmin) query = query.Where(x => x.OwnerTenantId == ot);
        if (from is { } f)
        {
            var fd = f.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.EntryDate >= fd);
        }
        if (to is { } t)
        {
            var td = t.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.EntryDate <= td);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(x => x.Title.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var parts = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeStatusToken).Where(x => x.Length > 0).ToList();
            if (parts.Count > 0) query = query.Where(x => parts.Contains(x.CurrentStatus));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query.OrderByDescending(x => x.EntryDate).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
            return new PagedResult<ComplaintListItemDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };

        var catIds = rows.Select(x => x.HelpdeskCategoryId).Distinct().ToList();
        var unitIds = rows.Select(x => x.UnitId).Distinct().ToList();
        var userIds = rows.Select(x => x.OwnerTenantId).Distinct().ToList();
        var cats = await db.HelpDeskCategories.AsNoTracking()
            .Where(x => catIds.Contains(x.IdHelpDeskCategory))
            .ToDictionaryAsync(x => x.IdHelpDeskCategory, x => x.HelpDeskCategoryName, cancellationToken).ConfigureAwait(false);
        var units = await db.Units.AsNoTracking()
            .Where(x => unitIds.Contains(x.IdUnit))
            .ToDictionaryAsync(x => x.IdUnit, x => x, cancellationToken).ConfigureAwait(false);
        var users = await db.Users.AsNoTracking()
            .Where(x => userIds.Contains(x.IdUser))
            .ToDictionaryAsync(x => x.IdUser, x => x.FullName, cancellationToken).ConfigureAwait(false);
        var types = await LoadOwnerTenantTypesAsync(apartmentId, userIds, cancellationToken).ConfigureAwait(false);

        var items = rows.Select(r =>
        {
            units.TryGetValue(r.UnitId, out var u);
            users.TryGetValue(r.OwnerTenantId, out var un);
            types.TryGetValue(r.OwnerTenantId, out var tp);
            return new ComplaintListItemDto
            {
                IdHelpDesk = r.IdHelpDesk,
                ComplaintCode = ComplaintCode(r.IdHelpDesk),
                Unit = new ComplaintUnitDto
                {
                    UnitId = r.UnitId,
                    UnitName = u?.UnitNumber ?? string.Empty,
                    Block = u?.Block
                },
                OwnerTenant = new ComplaintOwnerTenantDto
                {
                    Id = r.OwnerTenantId,
                    Name = un ?? string.Empty,
                    Type = tp ?? "User"
                },
                Category = new ComplaintCategoryMiniDto
                {
                    Id = r.HelpdeskCategoryId,
                    Name = cats.GetValueOrDefault(r.HelpdeskCategoryId) ?? string.Empty
                },
                Title = r.Title,
                Priority = r.Priority,
                EntryDate = r.EntryDate,
                CurrentStatus = r.CurrentStatus
            };
        }).ToList();

        return new PagedResult<ComplaintListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ComplaintDetailDto?> GetComplaintAsync(
        int apartmentId,
        int userId,
        bool isAdmin,
        int id,
        bool includeHistory,
        CancellationToken cancellationToken = default)
    {
        var r = await db.HelpDesks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdHelpDesk == id, cancellationToken)
            .ConfigureAwait(false);
        if (r is null) return null;
        if (!isAdmin && r.OwnerTenantId != userId) return null;

        var u = await db.Units.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdUnit == r.UnitId, cancellationToken).ConfigureAwait(false);
        var catName = await db.HelpDeskCategories.AsNoTracking()
            .Where(x => x.IdHelpDeskCategory == r.HelpdeskCategoryId)
            .Select(x => x.HelpDeskCategoryName)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var un = await db.Users.AsNoTracking().Where(x => x.IdUser == r.OwnerTenantId).Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var tp = (await LoadOwnerTenantTypesAsync(apartmentId, new[] { r.OwnerTenantId }.ToList(), cancellationToken).ConfigureAwait(false))
            .GetValueOrDefault(r.OwnerTenantId) ?? "User";

        IReadOnlyList<HelpdeskStatusEntryDto> history = [];
        if (includeHistory)
            history = await BuildHistoryAsync(id, cancellationToken).ConfigureAwait(false);

        return new ComplaintDetailDto
        {
            IdHelpDesk = r.IdHelpDesk,
            ComplaintCode = ComplaintCode(r.IdHelpDesk),
            Unit = new ComplaintUnitDto
            {
                UnitId = r.UnitId,
                UnitName = u?.UnitNumber ?? string.Empty,
                Block = u?.Block
            },
            OwnerTenant = new ComplaintOwnerTenantDto { Id = r.OwnerTenantId, Name = un, Type = tp },
            Category = new ComplaintCategoryMiniDto { Id = r.HelpdeskCategoryId, Name = catName },
            Title = r.Title,
            Priority = r.Priority,
            Description = r.Description,
            AgreementDocUrl = r.AgreementDocUrl,
            CurrentStatus = r.CurrentStatus,
            EntryDate = r.EntryDate,
            History = history
        };
    }

    public async Task<AppendComplaintStatusResponseDto> AppendStatusAsync(
        int apartmentId,
        int userId,
        int complaintId,
        AppendComplaintStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var note = (request.StatusDetails ?? string.Empty).Trim();
        if (note.Length is < 10 or > 1000) throw new InvalidOperationException("statusDetails must be 10..1000 characters.");
        var newStatus = (request.HelpdeskStatus ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(newStatus)) throw new InvalidOperationException("helpdeskStatus is required.");

        var hd = await db.HelpDesks
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdHelpDesk == complaintId, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Complaint not found.");

        if (!AllowedTransitions.TryGetValue(hd.CurrentStatus, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException("CONFLICT: Illegal status transition.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var row = new HelpdeskStatusDetail
        {
            IdHelpDesk = hd.IdHelpDesk,
            StatusEntryDate = now,
            StatusUpdatedBy = userId,
            StatusDetails = note,
            AttachmentDocUrl = string.IsNullOrWhiteSpace(request.AttachmentDocUrl) ? null : request.AttachmentDocUrl.Trim(),
            HelpdeskStatus = newStatus
        };
        db.HelpdeskStatusDetails.Add(row);
        hd.CurrentStatus = newStatus;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        var userName = await db.Users.AsNoTracking().Where(x => x.IdUser == userId).Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

        return new AppendComplaintStatusResponseDto
        {
            IdHelpDeskStatusDetails = row.IdHelpDeskStatusDetails,
            IdHelpDesk = hd.IdHelpDesk,
            HelpdeskStatus = newStatus,
            StatusEntryDate = now,
            StatusUpdatedBy = new StatusUpdatedByDto { UserId = userId, Name = userName },
            StatusDetails = note,
            AttachmentDocUrl = row.AttachmentDocUrl,
            ComplaintCurrentStatus = hd.CurrentStatus
        };
    }

    public async Task<IReadOnlyList<HelpdeskStatusEntryDto>> GetStatusTimelineAsync(
        int apartmentId,
        int userId,
        bool isAdmin,
        int complaintId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.HelpDesks.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.IdHelpDesk == complaintId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists) return [];
        if (!isAdmin)
        {
            var mine = await db.HelpDesks.AsNoTracking()
                .AnyAsync(x => x.ApartmentId == apartmentId && x.IdHelpDesk == complaintId && x.OwnerTenantId == userId,
                    cancellationToken).ConfigureAwait(false);
            if (!mine) return [];
        }
        return await BuildHistoryAsync(complaintId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<HelpdeskFileUploadDto> UploadFileAsync(
        int apartmentId,
        int userId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        _ = userId;
        if (file.Length == 0) throw new InvalidOperationException("file is required.");
        if (file.Length > MaxUploadBytes) throw new InvalidOperationException("FILE_TOO_LARGE");
        var ext = Path.GetExtension(file.FileName);
        var mime = string.IsNullOrWhiteSpace(file.ContentType) ? GuessMime(ext) : file.ContentType;
        if (!IsAllowedMime(mime, ext)) throw new InvalidOperationException("FILE_TYPE_NOT_ALLOWED");

        var now = DateTime.UtcNow;
        var key = $"helpdesk/{apartmentId}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(env.ContentRootPath, "Uploads", key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = File.Create(fullPath))
        {
            await file.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
        var url = "/uploads/" + key.Replace('\\', '/');
        return new HelpdeskFileUploadDto { Url = url, Size = file.Length, Mime = mime };
    }

    public async Task<HelpdeskStatsDto> GetStatsAsync(
        int apartmentId,
        int? month,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var m = month ?? today.Month;
        var y = year ?? today.Year;
        if (m is < 1 or > 12) throw new InvalidOperationException("month must be 1..12.");
        var monthStart = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var open = await db.HelpDesks.AsNoTracking()
            .CountAsync(x => x.ApartmentId == apartmentId && x.CurrentStatus == StatusOpen, cancellationToken)
            .ConfigureAwait(false);
        var inProg = await db.HelpDesks.AsNoTracking()
            .CountAsync(x => x.ApartmentId == apartmentId && x.CurrentStatus == StatusInProgress, cancellationToken)
            .ConfigureAwait(false);

        var resolvedThisMonth = await (
            from d in db.HelpdeskStatusDetails.AsNoTracking()
            join h in db.HelpDesks.AsNoTracking() on d.IdHelpDesk equals h.IdHelpDesk
            where h.ApartmentId == apartmentId && d.HelpdeskStatus == StatusResolved && d.StatusEntryDate >= monthStart &&
                  d.StatusEntryDate < monthEnd
            select d.IdHelpDeskStatusDetails).CountAsync(cancellationToken).ConfigureAwait(false);

        var closedThisMonth = await (
            from d in db.HelpdeskStatusDetails.AsNoTracking()
            join h in db.HelpDesks.AsNoTracking() on d.IdHelpDesk equals h.IdHelpDesk
            where h.ApartmentId == apartmentId && d.HelpdeskStatus == StatusClosed && d.StatusEntryDate >= monthStart &&
                  d.StatusEntryDate < monthEnd
            select d.IdHelpDeskStatusDetails).CountAsync(cancellationToken).ConfigureAwait(false);

        return new HelpdeskStatsDto
        {
            Open = open,
            InProgress = inProg,
            ResolvedThisMonth = resolvedThisMonth,
            ClosedThisMonth = closedThisMonth,
            AsOf = today
        };
    }

    public Task<StatusSummaryReportDto> GetStatusSummaryReportAsync(
        int apartmentId,
        DateOnly from,
        DateOnly to,
        int? categoryId,
        string? format,
        CancellationToken cancellationToken = default)
    {
        EnsureJsonFormat(format);
        return GetStatusSummaryReportInternalAsync(apartmentId, from, to, categoryId, cancellationToken);
    }

    private async Task<StatusSummaryReportDto> GetStatusSummaryReportInternalAsync(
        int apartmentId,
        DateOnly from,
        DateOnly to,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        var fd = from.ToDateTime(TimeOnly.MinValue);
        var td = to.ToDateTime(TimeOnly.MaxValue);
        var q = db.HelpDesks.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.EntryDate >= fd && x.EntryDate <= td);
        if (categoryId is { } c) q = q.Where(x => x.HelpdeskCategoryId == c);
        var rows = await q.ToListAsync(cancellationToken).ConfigureAwait(false);
        var total = rows.Count;
        if (total == 0)
            return new StatusSummaryReportDto { From = from, To = to, Rows = [], Total = 0 };

        var ordered = new[] { StatusOpen, StatusInProgress, StatusResolved, StatusClosed, StatusReopened };
        var groups = rows.GroupBy(x => x.CurrentStatus).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var result = new List<StatusSummaryRowDto>();
        foreach (var s in ordered)
        {
            var count = groups.TryGetValue(s, out var n) ? n : 0;
            result.Add(new StatusSummaryRowDto
            {
                Status = s,
                Count = count,
                Percent = total > 0 ? (int)Math.Round(100d * count / total) : 0
            });
        }
        foreach (var kv in groups.Where(k =>
                     !ordered.Any(o => string.Equals(o, k.Key, StringComparison.OrdinalIgnoreCase))))
            result.Add(new StatusSummaryRowDto
            {
                Status = kv.Key,
                Count = kv.Value,
                Percent = total > 0 ? (int)Math.Round(100d * kv.Value / total) : 0
            });

        return new StatusSummaryReportDto { From = from, To = to, Rows = result, Total = total };
    }

    public Task<CategoryBreakdownReportDto> GetCategoryBreakdownReportAsync(
        int apartmentId,
        DateOnly from,
        DateOnly to,
        int? categoryId,
        string? format,
        CancellationToken cancellationToken = default)
    {
        EnsureJsonFormat(format);
        return GetCategoryBreakdownInternalAsync(apartmentId, from, to, categoryId, cancellationToken);
    }

    private async Task<CategoryBreakdownReportDto> GetCategoryBreakdownInternalAsync(
        int apartmentId,
        DateOnly from,
        DateOnly to,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        var fd = from.ToDateTime(TimeOnly.MinValue);
        var td = to.ToDateTime(TimeOnly.MaxValue);
        var q = db.HelpDesks.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.EntryDate >= fd && x.EntryDate <= td);
        if (categoryId is { } c) q = q.Where(x => x.HelpdeskCategoryId == c);
        var rows = await q.ToListAsync(cancellationToken).ConfigureAwait(false);
        var catIds = rows.Select(x => x.HelpdeskCategoryId).Distinct().ToList();
        var catNames = await db.HelpDeskCategories.AsNoTracking()
            .Where(x => catIds.Contains(x.IdHelpDeskCategory))
            .ToDictionaryAsync(x => x.IdHelpDeskCategory, x => x.HelpDeskCategoryName, cancellationToken)
            .ConfigureAwait(false);

        var breakdown = rows.GroupBy(x => x.HelpdeskCategoryId).Select(g =>
        {
            var name = catNames.GetValueOrDefault(g.Key) ?? $"#{g.Key}";
            var list = g.ToList();
            var logged = list.Count;
            var resolved = list.Count(x =>
                string.Equals(x.CurrentStatus, StatusResolved, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.CurrentStatus, StatusClosed, StringComparison.OrdinalIgnoreCase));
            var open = list.Count(x => OpenWorkflowStatuses.Contains(x.CurrentStatus));
            return new CategoryBreakdownRowDto { Category = name, Logged = logged, Resolved = resolved, Open = open };
        }).OrderBy(x => x.Category).ToList();

        return new CategoryBreakdownReportDto { From = from, To = to, Rows = breakdown };
    }

    public Task<AgingReportDto> GetAgingReportAsync(
        int apartmentId,
        DateOnly? asOf,
        string? bucketsCsv,
        string? format,
        CancellationToken cancellationToken = default)
    {
        EnsureJsonFormat(format);
        return GetAgingInternalAsync(apartmentId, asOf, bucketsCsv, cancellationToken);
    }

    private async Task<AgingReportDto> GetAgingInternalAsync(
        int apartmentId,
        DateOnly? asOf,
        string? bucketsCsv,
        CancellationToken cancellationToken)
    {
        var ao = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var cuts = ParseBuckets(bucketsCsv ?? "7,15,30,60");
        var openComplaints = await db.HelpDesks.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId &&
                        (x.CurrentStatus == StatusOpen || x.CurrentStatus == StatusInProgress || x.CurrentStatus == StatusReopened))
            .Select(x => x.EntryDate)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var labels = BuildBucketLabels(cuts);
        var counts = new int[labels.Count];
        foreach (var entryDt in openComplaints)
        {
            var entry = DateOnly.FromDateTime(entryDt);
            var days = ao.DayNumber - entry.DayNumber;
            if (days < 0) days = 0;
            var idx = BucketIndex(days, cuts);
            counts[idx]++;
        }

        var buckets = labels.Select((label, i) => new AgingBucketDto { Label = label, Count = counts[i] }).ToList();
        return new AgingReportDto { AsOf = ao, Buckets = buckets };
    }

    private static void EnsureJsonFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return;
        var f = format.Trim().ToLowerInvariant();
        if (f is "json") return;
        if (f is "pdf" or "xlsx") throw new InvalidOperationException("EXPORT_FORMAT_NOT_IMPLEMENTED");
        throw new InvalidOperationException("Invalid format.");
    }

    private async Task<IReadOnlyList<HelpdeskStatusEntryDto>> BuildHistoryAsync(
        int complaintId, CancellationToken cancellationToken)
    {
        var rows = await db.HelpdeskStatusDetails.AsNoTracking()
            .Where(x => x.IdHelpDesk == complaintId)
            .OrderBy(x => x.StatusEntryDate)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var userIds = rows.Select(x => x.StatusUpdatedBy).Distinct().ToList();
        var users = await db.Users.AsNoTracking()
            .Where(x => userIds.Contains(x.IdUser))
            .ToDictionaryAsync(x => x.IdUser, x => x.FullName, cancellationToken).ConfigureAwait(false);
        return rows.Select(r => new HelpdeskStatusEntryDto
        {
            Id = r.IdHelpDeskStatusDetails,
            StatusEntryDate = r.StatusEntryDate,
            HelpdeskStatus = r.HelpdeskStatus,
            StatusUpdatedBy = new StatusUpdatedByDto
            {
                UserId = r.StatusUpdatedBy,
                Name = users.GetValueOrDefault(r.StatusUpdatedBy) ?? string.Empty
            },
            StatusDetails = r.StatusDetails,
            AttachmentDocUrl = r.AttachmentDocUrl
        }).ToList();
    }

    private async Task<Dictionary<int, string>> LoadOwnerTenantTypesAsync(
        int apartmentId, IReadOnlyList<int> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return [];
        var owners = await (
            from p in db.Persons.AsNoTracking()
            join pt in db.PersonTypes.AsNoTracking() on p.PersonTypeId equals pt.IdPersonType
            where p.ApartmentId == apartmentId && p.LinkedUserId != null && userIds.Contains(p.LinkedUserId!.Value)
            select new { Uid = p.LinkedUserId!.Value, pt.PersonTypeCode }
        ).ToListAsync(cancellationToken).ConfigureAwait(false);
        var map = new Dictionary<int, string>();
        foreach (var o in owners)
        {
            var label = o.PersonTypeCode.Equals(PersonTypeCodes.Owner, StringComparison.OrdinalIgnoreCase) ? "Owner"
                : o.PersonTypeCode.Equals(PersonTypeCodes.Tenant, StringComparison.OrdinalIgnoreCase) ? "Tenant"
                : "User";
            map[o.Uid] = label;
        }
        return map;
    }

    private async Task<int?> ResolveCallerUnitAsync(int apartmentId, int userId, CancellationToken cancellationToken)
    {
        var ownedUnit = await (
            from p in db.Persons.AsNoTracking()
            join uo in db.UnitOwners.AsNoTracking() on p.IdPerson equals uo.PersonId
            where p.ApartmentId == apartmentId && p.LinkedUserId == userId && p.IsActive && uo.ApartmentId == apartmentId && uo.IsActive
            select (int?)uo.UnitId
        ).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (ownedUnit is int ou) return ou;

        var tenantUnit = await (
            from p in db.Persons.AsNoTracking()
            join ta in db.TenantAssignments.AsNoTracking() on p.IdPerson equals ta.PersonId
            where p.ApartmentId == apartmentId && p.LinkedUserId == userId && p.IsActive && ta.ApartmentId == apartmentId && ta.IsActive
            select (int?)ta.UnitId
        ).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return tenantUnit;
    }

    private async Task<bool> UserHasUnitAccessAsync(int apartmentId, int userId, int unitId, CancellationToken cancellationToken)
    {
        var personIds = await db.Persons.AsNoTracking()
            .Where(p => p.ApartmentId == apartmentId && p.LinkedUserId == userId && p.IsActive)
            .Select(p => p.IdPerson)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (personIds.Count == 0) return false;
        if (await db.UnitOwners.AsNoTracking()
                .AnyAsync(uo => uo.ApartmentId == apartmentId && uo.UnitId == unitId && uo.IsActive && personIds.Contains(uo.PersonId),
                    cancellationToken).ConfigureAwait(false))
            return true;
        return await db.TenantAssignments.AsNoTracking()
            .AnyAsync(t => t.ApartmentId == apartmentId && t.UnitId == unitId && t.IsActive && personIds.Contains(t.PersonId),
                cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateComplaintRequest(LogComplaintRequest request)
    {
        var title = (request.Title ?? string.Empty).Trim();
        if (title.Length is < 1 or > 100) throw new InvalidOperationException("title must be 1..100 characters.");
        request.Title = title;
        var desc = (request.Description ?? string.Empty).Trim();
        if (desc.Length is < 20 or > 1000) throw new InvalidOperationException("description must be 20..1000 characters.");
        request.Description = desc;
        if (!AllowedPriorities.Contains(request.Priority))
            throw new InvalidOperationException("Invalid priority.");
        request.Priority = NormalizePriority(request.Priority);
    }

    private static string NormalizePriority(string p) =>
        AllowedPriorities.First(x => x.Equals(p, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeStatusToken(string s) => s.Trim();

    private static string ComplaintCode(int id) => $"CMP-{id:D4}";

    private static List<int> ParseBuckets(string csv)
    {
        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : (int?)null).Where(n => n > 0).Select(n => n!.Value).OrderBy(x => x).ToList();
        return parts.Count > 0 ? parts : new List<int> { 7, 15, 30, 60 };
    }

    private static List<string> BuildBucketLabels(IReadOnlyList<int> cuts)
    {
        var labels = new List<string>();
        var low = 0;
        foreach (var c in cuts)
        {
            labels.Add(low == 0 ? $"0–{c} days" : $"{low + 1}–{c} days");
            low = c;
        }
        labels.Add($"{low}+ days");
        return labels;
    }

    private static int BucketIndex(int days, IReadOnlyList<int> cuts)
    {
        if (days <= cuts[0]) return 0;
        for (var i = 1; i < cuts.Count; i++)
        {
            if (days <= cuts[i]) return i;
        }
        return cuts.Count;
    }

    private static bool IsAllowedMime(string mime, string ext)
    {
        if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) return true;
        if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        return false;
    }

    private static string GuessMime(string ext) => ext.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
