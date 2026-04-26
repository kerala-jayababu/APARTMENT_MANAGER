using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class VendorService(AppDbContext db) : IVendorService
{
    public async Task<IReadOnlyList<VendorDto>> ListVendorsForApartmentAsync(
        int apartmentId, CancellationToken cancellationToken = default)
    {
        var list = await db.Vendors.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId)
            .OrderBy(x => x.VendorName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<VendorDto?> GetByIdAsync(
        int idVendor, int apartmentId, CancellationToken cancellationToken = default)
    {
        var e = await db.Vendors.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IdVendor == idVendor && x.ApartmentId == apartmentId,
                cancellationToken)
            .ConfigureAwait(false);
        return e?.ToDto();
    }

    public async Task<EntitySaveResult<VendorDto>?> SaveAsync(
        VendorSaveDto dto, int userId, int apartmentId, CancellationToken cancellationToken = default)
    {
        if (dto.IdVendor <= 0)
        {
            var e = new Vendor
            {
                ApartmentId = apartmentId,
                VendorCode = dto.VendorCode.Trim(),
                VendorName = dto.VendorName.Trim(),
                VendorTypeId = dto.VendorTypeId,
                ContactPerson = string.IsNullOrWhiteSpace(dto.ContactPerson) ? null : dto.ContactPerson.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
                GstNumber = string.IsNullOrWhiteSpace(dto.GstNumber) ? null : dto.GstNumber.Trim(),
                PanNumber = string.IsNullOrWhiteSpace(dto.PanNumber) ? null : dto.PanNumber.Trim(),
                BankName = string.IsNullOrWhiteSpace(dto.BankName) ? null : dto.BankName.Trim(),
                BankAccountNumber = string.IsNullOrWhiteSpace(dto.BankAccountNumber) ? null : dto.BankAccountNumber.Trim(),
                IfscCode = string.IsNullOrWhiteSpace(dto.IfscCode) ? null : dto.IfscCode.Trim(),
                AddressLine1 = string.IsNullOrWhiteSpace(dto.AddressLine1) ? null : dto.AddressLine1.Trim(),
                AddressLine2 = string.IsNullOrWhiteSpace(dto.AddressLine2) ? null : dto.AddressLine2.Trim(),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ControlLedgerAccountId = dto.ControlLedgerAccountId,
                OpeningPayable = dto.OpeningPayable
            };
            db.Vendors.Add(e);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new EntitySaveResult<VendorDto> { Data = e.ToDto(), Created = true };
        }

        var existing = await db.Vendors
            .FirstOrDefaultAsync(
                x => x.IdVendor == dto.IdVendor && x.ApartmentId == apartmentId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return null;

        existing.VendorCode = dto.VendorCode.Trim();
        existing.VendorName = dto.VendorName.Trim();
        existing.VendorTypeId = dto.VendorTypeId;
        existing.ContactPerson = string.IsNullOrWhiteSpace(dto.ContactPerson) ? null : dto.ContactPerson.Trim();
        existing.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        existing.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        existing.GstNumber = string.IsNullOrWhiteSpace(dto.GstNumber) ? null : dto.GstNumber.Trim();
        existing.PanNumber = string.IsNullOrWhiteSpace(dto.PanNumber) ? null : dto.PanNumber.Trim();
        existing.BankName = string.IsNullOrWhiteSpace(dto.BankName) ? null : dto.BankName.Trim();
        existing.BankAccountNumber = string.IsNullOrWhiteSpace(dto.BankAccountNumber) ? null : dto.BankAccountNumber.Trim();
        existing.IfscCode = string.IsNullOrWhiteSpace(dto.IfscCode) ? null : dto.IfscCode.Trim();
        existing.AddressLine1 = string.IsNullOrWhiteSpace(dto.AddressLine1) ? null : dto.AddressLine1.Trim();
        existing.AddressLine2 = string.IsNullOrWhiteSpace(dto.AddressLine2) ? null : dto.AddressLine2.Trim();
        existing.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        existing.IsActive = dto.IsActive;
        existing.ControlLedgerAccountId = dto.ControlLedgerAccountId;
        existing.OpeningPayable = dto.OpeningPayable;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new EntitySaveResult<VendorDto> { Data = existing.ToDto(), Created = false };
    }
}
