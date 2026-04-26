using System.Security.Claims;
using Apartment_API.Helpers;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Apartment_API.Services.Implementation;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? IdUser =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public string? FullName => Principal?.FindFirstValue(ClaimTypes.Name);

    public string? PhoneNumber => Principal?.FindFirstValue(ClaimTypesExtra.Phone);

    public bool IsSuperAdmin => Principal?.IsInRole("SuperAdmin") == true;

    public string? TokenPurpose => Principal?.FindFirstValue(ClaimTypesExtra.TokenPurpose);

    public int? IdApartment =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypesExtra.ApartmentId), out var aid) ? aid : null;

    public string? ApartmentName => Principal?.FindFirstValue(ClaimTypesExtra.ApartmentName);

    public int? ApartmentUserRoleId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypesExtra.ApartmentUserRoleId), out var r) ? r : null;
}
