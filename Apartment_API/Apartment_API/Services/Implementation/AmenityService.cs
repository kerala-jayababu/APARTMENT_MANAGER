using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Apartment_API.Services.Implementation;

public sealed class AmenityService(AppDbContext db) : IAmenityService
{
    public async Task<IReadOnlyList<AmenityListDto>> ListAsync(
        int apartmentId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var q = db.Amenities.AsNoTracking().Where(a => a.ApartmentId == apartmentId);
        if (isActive is { } act) q = q.Where(a => a.IsActive == act);
        var rows = await q.OrderBy(a => a.AmenityName).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0) return [];

        var amenityTypeIds = rows.Select(x => x.AmenityTypeId).Distinct().ToList();
        var incomeHeadIds = rows.Select(x => x.IncomeHeadId).Distinct().ToList();
        var chargeTypeIds = rows.Select(x => x.ChargeTypeId).Distinct().ToList();

        var typeNames = await db.AmenityTypes.AsNoTracking()
            .Where(x => amenityTypeIds.Contains(x.IdAmenityType))
            .ToDictionaryAsync(x => x.IdAmenityType, x => x.AmenityTypeName, cancellationToken)
            .ConfigureAwait(false);
        var incomeNames = await db.IncomeHeads.AsNoTracking()
            .Where(x => incomeHeadIds.Contains(x.IdIncomeHead))
            .ToDictionaryAsync(x => x.IdIncomeHead, x => x.HeadName, cancellationToken)
            .ConfigureAwait(false);
        var chargeNames = await db.BookingChargeTypes.AsNoTracking()
            .Where(x => chargeTypeIds.Contains(x.IdBookingChargeType))
            .ToDictionaryAsync(x => x.IdBookingChargeType, x => x.ChargeTypeName, cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(x => new AmenityListDto
        {
            Id = x.IdAmenity,
            AmenityTypeId = x.AmenityTypeId,
            AmenityTypeName = typeNames.GetValueOrDefault(x.AmenityTypeId) ?? string.Empty,
            IncomeHeadId = x.IncomeHeadId,
            IncomeHeadName = incomeNames.GetValueOrDefault(x.IncomeHeadId) ?? string.Empty,
            AmenityName = x.AmenityName,
            Description = x.Description,
            Capacity = x.Capacity,
            ChargeTypeId = x.ChargeTypeId,
            ChargeTypeName = chargeNames.GetValueOrDefault(x.ChargeTypeId) ?? string.Empty,
            ChargeAmount = x.ChargeAmount,
            AdvanceBookingDays = x.AdvanceBookingDays,
            MaxContinuousSessionsAllowed = x.MaxContinuousSessionsAllowed,
            ImageUrl = x.ImageUrl,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<AmenityListDto?> GetByIdAsync(int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var x = await db.Amenities.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && a.IdAmenity == id, cancellationToken)
            .ConfigureAwait(false);
        if (x is null) return null;

        var typeName = await db.AmenityTypes.AsNoTracking()
            .Where(t => t.IdAmenityType == x.AmenityTypeId)
            .Select(t => t.AmenityTypeName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var incomeName = await db.IncomeHeads.AsNoTracking()
            .Where(h => h.IdIncomeHead == x.IncomeHeadId)
            .Select(h => h.HeadName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var chargeName = await db.BookingChargeTypes.AsNoTracking()
            .Where(c => c.IdBookingChargeType == x.ChargeTypeId)
            .Select(c => c.ChargeTypeName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AmenityListDto
        {
            Id = x.IdAmenity,
            AmenityTypeId = x.AmenityTypeId,
            AmenityTypeName = typeName ?? string.Empty,
            IncomeHeadId = x.IncomeHeadId,
            IncomeHeadName = incomeName ?? string.Empty,
            AmenityName = x.AmenityName,
            Description = x.Description,
            Capacity = x.Capacity,
            ChargeTypeId = x.ChargeTypeId,
            ChargeTypeName = chargeName ?? string.Empty,
            ChargeAmount = x.ChargeAmount,
            AdvanceBookingDays = x.AdvanceBookingDays,
            MaxContinuousSessionsAllowed = x.MaxContinuousSessionsAllowed,
            ImageUrl = x.ImageUrl,
            IsActive = x.IsActive
        };
    }

    public async Task<int> CreateAsync(
        int apartmentId, int userId, CreateAmenityRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(apartmentId, request, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(request.AmenityName)) throw new InvalidOperationException("AmenityName is required.");
        var name = request.AmenityName.Trim();
        var exists = await db.Amenities.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.AmenityName == name && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (exists) throw new InvalidOperationException("Amenity name already exists.");

        var now = DateTime.UtcNow;
        var row = new Amenity
        {
            ApartmentId = apartmentId,
            AmenityTypeId = request.AmenityTypeId,
            IncomeHeadId = request.IncomeHeadId,
            AmenityName = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description!.Trim(),
            Capacity = request.Capacity,
            ChargeTypeId = request.ChargeTypeId,
            ChargeAmount = request.ChargeAmount,
            AdvanceBookingDays = request.AdvanceBookingDays,
            MaxContinuousSessionsAllowed = request.MaxContinuousSessionsAllowed,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl!.Trim(),
            IsActive = request.IsActive,
            CreatedAt = now,
            CreatedBy = userId
        };
        db.Amenities.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return row.IdAmenity;
    }

    public async Task UpdateAsync(
        int apartmentId, int userId, int id, CreateAmenityRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(apartmentId, request, cancellationToken).ConfigureAwait(false);
        var row = await db.Amenities
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdAmenity == id, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) throw new InvalidOperationException("Amenity not found.");
        if (string.IsNullOrWhiteSpace(request.AmenityName)) throw new InvalidOperationException("AmenityName is required.");
        var name = request.AmenityName.Trim();
        var duplicate = await db.Amenities.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.IdAmenity != id && x.AmenityName == name && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate) throw new InvalidOperationException("Amenity name already exists.");

        row.AmenityTypeId = request.AmenityTypeId;
        row.IncomeHeadId = request.IncomeHeadId;
        row.AmenityName = name;
        row.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description!.Trim();
        row.Capacity = request.Capacity;
        row.ChargeTypeId = request.ChargeTypeId;
        row.ChargeAmount = request.ChargeAmount;
        row.AdvanceBookingDays = request.AdvanceBookingDays;
        row.MaxContinuousSessionsAllowed = request.MaxContinuousSessionsAllowed;
        row.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl!.Trim();
        row.IsActive = request.IsActive;
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AmenityBookingListDto>> ListBookingsAsync(
        int apartmentId,
        string? search,
        int? amenityId,
        int? statusId,
        CancellationToken cancellationToken = default)
    {
        var q =
            from b in db.AmenityBookings.AsNoTracking()
            join a in db.Amenities.AsNoTracking() on b.AmenityId equals a.IdAmenity
            join u in db.Units.AsNoTracking() on b.UnitId equals u.IdUnit
            join p in db.Persons.AsNoTracking() on b.PersonId equals p.IdPerson into personJoin
            from p in personJoin.DefaultIfEmpty()
            join s in db.InvoiceStatuses.AsNoTracking() on b.StatusId equals s.IdInvoiceStatus into statusJoin
            from s in statusJoin.DefaultIfEmpty()
            where b.ApartmentId == apartmentId
            select new { b, a, u, p, s };

        if (amenityId is { } aid) q = q.Where(x => x.b.AmenityId == aid);
        if (statusId is { } sid) q = q.Where(x => x.b.StatusId == sid);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(x =>
                x.u.UnitNumber.Contains(term) ||
                x.a.AmenityName.Contains(term) ||
                (x.p != null && x.p.FullName.Contains(term)));
        }

        var rows = await q
            .OrderByDescending(x => x.b.BookingDate)
            .ThenByDescending(x => x.b.SlotStartTime)
            .ThenByDescending(x => x.b.IdAmenityBooking)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(x => new AmenityBookingListDto
        {
            Id = x.b.IdAmenityBooking,
            BookingCode = BuildBookingCode(x.b),
            UnitId = x.b.UnitId,
            UnitNumber = x.u.UnitNumber,
            PersonId = x.b.PersonId,
            OwnerName = x.p?.FullName,
            OwnerPhone = x.p?.PhoneNumber,
            AmenityId = x.b.AmenityId,
            AmenityName = x.a.AmenityName,
            BookingDate = x.b.BookingDate,
            SlotStartTime = x.b.SlotStartTime,
            SlotEndTime = x.b.SlotEndTime,
            ChargeAmount = x.b.ChargeAmount,
            InvoiceId = x.b.InvoiceId,
            StatusId = x.b.StatusId,
            StatusName = x.s?.StatusName,
            Notes = x.b.Notes,
            IsActive = x.b.IsActive
        }).ToList();
    }

    public async Task<int> CreateBookingAsync(
        int apartmentId,
        int userId,
        CreateAmenityBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateBookingRequest(request);
        var amenity = await LoadAmenityAsync(apartmentId, request.AmenityId, cancellationToken).ConfigureAwait(false);
        await ValidateUnitAndPersonAsync(apartmentId, request.UnitId, request.PersonId, cancellationToken).ConfigureAwait(false);
        await EnsureNoSlotConflictAsync(apartmentId, request.AmenityId, null, request.BookingDate, request.SlotStartTime, request.SlotEndTime, cancellationToken).ConfigureAwait(false);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var finalStatusId = request.StatusId ?? await ResolvePendingStatusIdAsync(cancellationToken).ConfigureAwait(false);
        var invoice = await BuildInvoiceEntityAsync(
            apartmentId,
            userId,
            request,
            amenity,
            finalStatusId,
            now,
            cancellationToken).ConfigureAwait(false);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var booking = new AmenityBooking
        {
            ApartmentId = apartmentId,
            AmenityId = request.AmenityId,
            UnitId = request.UnitId,
            PersonId = request.PersonId,
            BookingDate = request.BookingDate.Date,
            SlotStartTime = request.SlotStartTime,
            SlotEndTime = request.SlotEndTime,
            ChargeAmount = request.ChargeAmount,
            InvoiceId = invoice.IdInvoice,
            Notes = NormalizeNotes(request.Notes),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
            StatusId = finalStatusId
        };
        db.AmenityBookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return booking.IdAmenityBooking;
    }

    public async Task UpdateBookingAsync(
        int apartmentId,
        int userId,
        int id,
        CreateAmenityBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateBookingRequest(request);
        var booking = await db.AmenityBookings
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdAmenityBooking == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (booking is null) throw new InvalidOperationException("Amenity booking not found.");

        var amenity = await LoadAmenityAsync(apartmentId, request.AmenityId, cancellationToken).ConfigureAwait(false);
        await ValidateUnitAndPersonAsync(apartmentId, request.UnitId, request.PersonId, cancellationToken).ConfigureAwait(false);
        await EnsureNoSlotConflictAsync(apartmentId, request.AmenityId, id, request.BookingDate, request.SlotStartTime, request.SlotEndTime, cancellationToken).ConfigureAwait(false);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var finalStatusId = request.StatusId ?? booking.StatusId ?? await ResolvePendingStatusIdAsync(cancellationToken).ConfigureAwait(false);
        booking.AmenityId = request.AmenityId;
        booking.UnitId = request.UnitId;
        booking.PersonId = request.PersonId;
        booking.BookingDate = request.BookingDate.Date;
        booking.SlotStartTime = request.SlotStartTime;
        booking.SlotEndTime = request.SlotEndTime;
        booking.ChargeAmount = request.ChargeAmount;
        booking.Notes = NormalizeNotes(request.Notes);
        booking.StatusId = finalStatusId;
        booking.UpdatedAt = now;
        booking.UpdatedBy = userId;

        if (booking.InvoiceId is { } invoiceId)
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(
                    x => x.IdInvoice == invoiceId && x.ApartmentId == apartmentId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (invoice is not null)
            {
                UpdateInvoiceEntity(invoice, booking, amenity, finalStatusId, userId, now);
            }
            else
            {
                var newInvoice = await BuildInvoiceEntityAsync(
                    apartmentId,
                    userId,
                    request,
                    amenity,
                    finalStatusId,
                    now,
                    cancellationToken).ConfigureAwait(false);
                db.Invoices.Add(newInvoice);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                booking.InvoiceId = newInvoice.IdInvoice;
            }
        }
        else
        {
            var newInvoice = await BuildInvoiceEntityAsync(
                apartmentId,
                userId,
                request,
                amenity,
                finalStatusId,
                now,
                cancellationToken).ConfigureAwait(false);
            db.Invoices.Add(newInvoice);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            booking.InvoiceId = newInvoice.IdInvoice;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelBookingAsync(
        int apartmentId,
        int userId,
        int id,
        CancelAmenityBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var booking = await db.AmenityBookings
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdAmenityBooking == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (booking is null) throw new InvalidOperationException("Amenity booking not found.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (booking.InvoiceId is { } invoiceId)
        {
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdInvoice == invoiceId, cancellationToken)
                .ConfigureAwait(false);
            if (invoice is not null) db.Invoices.Remove(invoice);
        }

        var cancelStatusId = await ResolveCancelledStatusIdAsync(cancellationToken).ConfigureAwait(false);
        booking.IsActive = false;
        booking.StatusId = cancelStatusId ?? booking.StatusId;
        booking.InvoiceId = null;
        booking.Notes = string.IsNullOrWhiteSpace(request.Reason)
            ? booking.Notes
            : NormalizeNotes($"Cancelled: {request.Reason.Trim()}");
        booking.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateReferencesAsync(int apartmentId, CreateAmenityRequest request, CancellationToken cancellationToken)
    {
        var typeOk = await db.AmenityTypes.AsNoTracking()
            .AnyAsync(x => x.IdAmenityType == request.AmenityTypeId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (!typeOk) throw new InvalidOperationException("Invalid AmenityTypeId.");

        var incomeOk = await db.IncomeHeads.AsNoTracking()
            .AnyAsync(x => x.IdIncomeHead == request.IncomeHeadId && x.IsActive && (x.ApartmentId == null || x.ApartmentId == apartmentId), cancellationToken)
            .ConfigureAwait(false);
        if (!incomeOk) throw new InvalidOperationException("Invalid IncomeHeadId.");

        var chargeOk = await db.BookingChargeTypes.AsNoTracking()
            .AnyAsync(x => x.IdBookingChargeType == request.ChargeTypeId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (!chargeOk) throw new InvalidOperationException("Invalid ChargeTypeId.");

        if (request.MaxContinuousSessionsAllowed <= 0)
            throw new InvalidOperationException("MaxContinuousSessionsAllowed must be greater than 0.");
        if (request.ChargeAmount < 0)
            throw new InvalidOperationException("ChargeAmount cannot be negative.");
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 2000)
            throw new InvalidOperationException("Description can be at most 2000 characters.");
    }

    private static string BuildBookingCode(AmenityBooking booking) =>
        $"BK-{booking.BookingDate.Year}-{booking.IdAmenityBooking:D4}";

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        var value = notes.Trim();
        if (value.Length > 1000) throw new InvalidOperationException("Notes can be at most 1000 characters.");
        return value;
    }

    private static void ValidateBookingRequest(CreateAmenityBookingRequest request)
    {
        if (request.AmenityId <= 0) throw new InvalidOperationException("AmenityId is required.");
        if (request.UnitId <= 0) throw new InvalidOperationException("UnitId is required.");
        if (request.PersonId <= 0) throw new InvalidOperationException("PersonId is required.");
        if (request.ChargeAmount < 0) throw new InvalidOperationException("ChargeAmount cannot be negative.");
        if (request.SlotStartTime >= request.SlotEndTime) throw new InvalidOperationException("SlotEndTime must be greater than SlotStartTime.");
        _ = NormalizeNotes(request.Notes);
    }

    private async Task<Amenity> LoadAmenityAsync(int apartmentId, int amenityId, CancellationToken cancellationToken)
    {
        var amenity = await db.Amenities.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdAmenity == amenityId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        return amenity ?? throw new InvalidOperationException("Amenity not found or inactive.");
    }

    private async Task ValidateUnitAndPersonAsync(int apartmentId, int unitId, int personId, CancellationToken cancellationToken)
    {
        var unitOk = await db.Units.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.IdUnit == unitId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (!unitOk) throw new InvalidOperationException("Invalid UnitId.");

        var personOk = await db.Persons.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.IdPerson == personId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (!personOk) throw new InvalidOperationException("Invalid PersonId.");
    }

    private async Task EnsureNoSlotConflictAsync(
        int apartmentId,
        int amenityId,
        int? bookingIdToExclude,
        DateTime bookingDate,
        TimeSpan slotStartTime,
        TimeSpan slotEndTime,
        CancellationToken cancellationToken)
    {
        var date = bookingDate.Date;
        var conflict = await db.AmenityBookings.AsNoTracking()
            .AnyAsync(x =>
                x.ApartmentId == apartmentId &&
                x.AmenityId == amenityId &&
                x.IsActive &&
                x.BookingDate == date &&
                (bookingIdToExclude == null || x.IdAmenityBooking != bookingIdToExclude.Value) &&
                slotStartTime < x.SlotEndTime &&
                slotEndTime > x.SlotStartTime,
                cancellationToken)
            .ConfigureAwait(false);
        if (conflict) throw new InvalidOperationException("The selected slot overlaps an existing booking.");
    }

    private async Task<Invoice> BuildInvoiceEntityAsync(
        int apartmentId,
        int userId,
        CreateAmenityBookingRequest request,
        Amenity amenity,
        int statusId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var fiscalYearId = await ResolveFiscalYearIdAsync(apartmentId, request.BookingDate.Date, cancellationToken).ConfigureAwait(false);
        return new Invoice
        {
            ApartmentId = apartmentId,
            InvoiceSource = "AMENITY_BOOKING",
            UnitId = request.UnitId,
            PersonId = request.PersonId,
            IncomeHeadId = amenity.IncomeHeadId,
            FiscalYearId = fiscalYearId,
            InvoicePeriodStart = request.BookingDate.Date,
            InvoicePeriodEnd = request.BookingDate.Date,
            InvoiceDate = now.Date,
            DueDate = now.Date,
            Amount = request.ChargeAmount,
            GstAmount = 0m,
            TotalAmount = request.ChargeAmount,
            PaidAmount = 0m,
            BalanceAmount = request.ChargeAmount,
            StatusId = statusId,
            IsAutoGenerated = true,
            Notes = NormalizeNotes(request.Notes),
            InvoiceNumber = BuildInvoiceNumber(apartmentId, request.BookingDate.Date, now),
            CreatedAt = now,
            CreatedBy = userId
        };
    }

    private void UpdateInvoiceEntity(
        Invoice invoice,
        AmenityBooking booking,
        Amenity amenity,
        int statusId,
        int userId,
        DateTime now)
    {
        invoice.UnitId = booking.UnitId;
        invoice.PersonId = booking.PersonId;
        invoice.IncomeHeadId = amenity.IncomeHeadId;
        invoice.InvoicePeriodStart = booking.BookingDate.Date;
        invoice.InvoicePeriodEnd = booking.BookingDate.Date;
        invoice.InvoiceDate = now.Date;
        invoice.DueDate = now.Date;
        invoice.Amount = booking.ChargeAmount;
        invoice.GstAmount = 0m;
        invoice.TotalAmount = booking.ChargeAmount;
        invoice.BalanceAmount = booking.ChargeAmount - invoice.PaidAmount;
        invoice.StatusId = statusId;
        invoice.Notes = booking.Notes;
        invoice.UpdatedAt = now;
        invoice.UpdatedBy = userId;
    }

    private async Task<int> ResolveFiscalYearIdAsync(int apartmentId, DateTime date, CancellationToken cancellationToken)
    {
        var fiscalYearId = await db.FiscalYears.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && (x.IsActive ?? true) && x.StartDate <= date && x.EndDate >= date)
            .OrderByDescending(x => x.IsCurrent)
            .ThenByDescending(x => x.StartDate)
            .Select(x => (int?)x.IdFiscalYear)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (fiscalYearId is not { } id) throw new InvalidOperationException("No active fiscal year found for booking date.");
        return id;
    }

    private async Task<int> ResolvePendingStatusIdAsync(CancellationToken cancellationToken)
    {
        var statusId = await db.InvoiceStatuses.AsNoTracking()
            .Where(x => x.IsActive && (x.StatusCode == "PENDING" || x.StatusCode == "PENDING_PAYMENT"))
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.IdInvoiceStatus)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (statusId is not { } id) throw new InvalidOperationException("Pending invoice status is not configured.");
        return id;
    }

    private async Task<int?> ResolveCancelledStatusIdAsync(CancellationToken cancellationToken)
    {
        return await db.InvoiceStatuses.AsNoTracking()
            .Where(x => x.IsActive && (x.StatusCode == "CANCELLED" || x.StatusCode == "CANCELED"))
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.IdInvoiceStatus)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string BuildInvoiceNumber(int apartmentId, DateTime bookingDate, DateTime nowUtc)
    {
        var stamp = nowUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"INV-AB-{apartmentId}-{bookingDate:yyyyMMdd}-{stamp}";
    }
}
