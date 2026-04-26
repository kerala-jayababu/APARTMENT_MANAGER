using Apartment_API.DTO;
using Apartment_API.Models;

namespace Apartment_API.Helpers;

public static class UserMappingExtensions
{
    public static UserPublicDto ToPublicDto(this User u) => new()
    {
        IdUser = u.IdUser,
        FullName = u.FullName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        IsActive = u.IsActive,
        LastLoginAt = u.LastLoginAt,
        Designation = u.Designation,
        ProfilePhotoUrl = u.ProfilePhotoUrl,
        IsSuperAdmin = u.IsSuperAdmin
    };
}
