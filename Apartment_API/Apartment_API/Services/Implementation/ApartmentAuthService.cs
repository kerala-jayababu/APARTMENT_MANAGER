using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ApartmentAuthService(
    AppDbContext db,
    IJwtTokenService jwtTokenService) : IApartmentAuthService
{
    public async Task<IReadOnlyList<AvailableApartmentItemDto>> GetApartmentsForUserAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        var list = await (
                from au in db.ApartmentUsers.AsNoTracking()
                join a in db.Apartments.AsNoTracking() on au.ApartmentId equals a.IdApartment
                where au.UserId == userId && au.IsActive && a.IsActive
                orderby a.ApartmentName
                select new AvailableApartmentItemDto
                {
                    IdApartment = a.IdApartment,
                    ApartmentCode = a.ApartmentCode,
                    ApartmentName = a.ApartmentName,
                    AssociationName = a.AssociationName,
                    RoleId = au.RoleId,
                    IdApartmentUser = au.IdApartmentUser
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    public async Task<(TenantTokenResponseDto? data, string? errorCode)> CreateTenantAccessTokenAsync(
        int userId,
        int apartmentId,
        CancellationToken cancellationToken = default)
    {
        if (apartmentId <= 0)
            return (null, "INVALID_APARTMENT_ID");

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUser == userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return (null, "USER_INACTIVE_OR_MISSING");

        var hasAnyMembership = await db.ApartmentUsers
            .AnyAsync(au => au.UserId == userId && au.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (!hasAnyMembership)
            return (null, "NO_APARTMENT_MEMBERSHIP");

        var map = await db.ApartmentUsers
            .AsNoTracking()
            .Where(au => au.UserId == userId && au.ApartmentId == apartmentId && au.IsActive)
            .OrderBy(au => au.IdApartmentUser)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (map is null)
            return (null, "NOT_MAPPED_TO_APARTMENT");

        var apartment = await db.Apartments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdApartment == apartmentId && a.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (apartment is null)
            return (null, "APARTMENT_INACTIVE_OR_MISSING");

        var publicUser = user.ToPublicDto();

        var ctx = new ApartmentTokenContext
        {
            IdApartment = apartment.IdApartment,
            ApartmentName = apartment.ApartmentName,
            RoleId = map.RoleId
        };

        var (accessToken, expires) = jwtTokenService.CreateApartmentAccessToken(publicUser, ctx);
        return (new TenantTokenResponseDto
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = expires,
            IdUser = user.IdUser,
            IdApartment = apartment.IdApartment,
            ApartmentName = apartment.ApartmentName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName
        }, null);
    }
}
