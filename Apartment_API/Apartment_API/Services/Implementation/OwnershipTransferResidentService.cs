using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class OwnershipTransferResidentService(AppDbContext db, IWebHostEnvironment env)
    : ResidentServiceBase(db, env), IOwnershipTransferResidentService
{
    public async Task<OwnershipTransferCreatedDto> RecordAsync(
        int apartmentId, int userId, RecordOwnershipTransferRequest request, CancellationToken cancellationToken = default)
    {
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        var u = await Db.Units.FirstOrDefaultAsync(x => x.IdUnit == request.UnitId && x.ApartmentId == apartmentId, cancellationToken);
        if (u is null) throw new InvalidOperationException("Unit not found.");
        int? prev = null;
        var oldP = await Db.UnitOwners
            .Where(x => x.ApartmentId == apartmentId && x.UnitId == request.UnitId && x.IsPrimaryOwner && x.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var o in oldP)
        {
            o.IsActive = false;
            o.OwnershipToDate = request.TransferDate.Date;
            o.UpdatedAt = DateTime.UtcNow;
            o.UpdatedBy = userId;
            prev = o.PersonId;
        }
        var now = DateTime.UtcNow;
        var transferValue = request.TransferValue ?? 0m;
        Db.UnitOwners.Add(new UnitOwner
        {
            ApartmentId = apartmentId,
            UnitId = request.UnitId,
            PersonId = request.NewOwnerPersonId,
            IsPrimaryOwner = true,
            OwnershipFromDate = request.TransferDate.Date,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
            TransferValue = transferValue
        });
        u.IdCurrentOwner = request.NewOwnerPersonId;
        u.UpdatedAt = now;
        u.UpdatedBy = userId;
        var oh = new OwnershipHistory
        {
            ApartmentId = apartmentId,
            UnitId = request.UnitId,
            PreviousOwnerPersonId = prev,
            NewOwnerPersonId = request.NewOwnerPersonId,
            TransferType = request.TransferType,
            TransferDate = request.TransferDate.Date,
            SaleDeedReference = request.SaleDeedReference,
            TransferValue = transferValue,
            Remarks = request.Remarks,
            RecordedByUserId = userId,
            RecordedAt = now
        };
        Db.OwnershipHistory.Add(oh);
        await Db.SaveChangesAsync(cancellationToken);
        var newUo = await Db.UnitOwners
            .OrderByDescending(x => x.IdUnitOwner)
            .FirstAsync(x => x.UnitId == request.UnitId && x.PersonId == request.NewOwnerPersonId, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return new OwnershipTransferCreatedDto { Id = oh.IdOwnershipHistory, NewPrimaryUnitOwnerId = newUo.IdUnitOwner };
    }

    public async Task<IReadOnlyList<OwnershipHistoryItemDto>> GetHistoryForUnitAsync(
        int apartmentId, int unitId, CancellationToken cancellationToken = default)
    {
        return await (from h in Db.OwnershipHistory.AsNoTracking()
                where h.ApartmentId == apartmentId && h.UnitId == unitId
                join newP in Db.Persons.AsNoTracking() on h.NewOwnerPersonId equals newP.IdPerson
                join prevP in Db.Persons.AsNoTracking() on h.PreviousOwnerPersonId equals prevP.IdPerson into prevJ
                from prevP in prevJ.DefaultIfEmpty()
                join d in Db.StoredDocuments.AsNoTracking() on h.DeedDocumentId equals d.IdDocument into dJ
                from d in dJ.DefaultIfEmpty()
                orderby h.RecordedAt descending
                select new OwnershipHistoryItemDto
                {
                    Id = h.IdOwnershipHistory,
                    UnitId = h.UnitId,
                    PreviousOwnerPersonId = h.PreviousOwnerPersonId,
                    PreviousOwnerName = prevP != null ? prevP.FullName : null,
                    NewOwnerPersonId = h.NewOwnerPersonId,
                    NewOwnerName = newP.FullName,
                    TransferType = h.TransferType,
                    TransferDate = h.TransferDate,
                    SaleDeedReference = h.SaleDeedReference,
                    TransferValue = h.TransferValue,
                    DeedDocumentUrl = d != null ? d.FileUrl : null,
                    RecordedAt = h.RecordedAt
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<IdProofResultDto> UploadDeedAsync(
        int apartmentId, int userId, int idOwnershipHistory, Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var h = await Db.OwnershipHistory
            .FirstOrDefaultAsync(
                x => x.IdOwnershipHistory == idOwnershipHistory && x.ApartmentId == apartmentId, cancellationToken);
        if (h is null) throw new InvalidOperationException("Ownership history not found.");
        var catId = await Db.DocumentCategories.AsNoTracking()
            .Where(c => c.IsActive && c.CategoryCode == "SALE_DEED")
            .Select(c => c.IdDocumentCategory)
            .FirstOrDefaultAsync(cancellationToken);
        if (catId == 0)
            catId = await Db.DocumentCategories.AsNoTracking().Select(c => c.IdDocumentCategory).FirstAsync(cancellationToken);
        var url = UploadFile(fileStream, apartmentId, fileName);
        var doc = new StoredDocument
        {
            ApartmentId = apartmentId,
            CategoryId = catId,
            DocumentName = Path.GetFileName(fileName),
            FileUrl = url,
            LinkedEntityType = "OwnershipHistory",
            LinkedEntityId = idOwnershipHistory,
            UploadedByUserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        Db.StoredDocuments.Add(doc);
        await Db.SaveChangesAsync(cancellationToken);
        h.DeedDocumentId = doc.IdDocument;
        await Db.SaveChangesAsync(cancellationToken);
        return new IdProofResultDto { DocumentId = doc.IdDocument, FileUrl = url };
    }
}
