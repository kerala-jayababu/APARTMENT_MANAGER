namespace Apartment_API.Services.Interfaces;

/// <summary>
/// Reads the currently authenticated user from the JWT (HttpContext.User claims).
/// Inject into any <c>[Authorize]</c> controller or service.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? IdUser { get; }
    string? Email { get; }
    string? FullName { get; }
    string? PhoneNumber { get; }
    bool IsSuperAdmin { get; }
}
