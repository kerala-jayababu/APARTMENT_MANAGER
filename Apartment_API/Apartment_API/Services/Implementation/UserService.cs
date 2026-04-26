using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class UserService(
    AppDbContext db,
    IPasswordHasher passwordHasher) : IUserService
{
    private const int SystemCreatedBy = 0;

    public async Task<(UserPublicDto user, string? errorCode)> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false))
            return (default!, "EMAIL_ALREADY_EXISTS");

        var entity = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            IsSuperAdmin = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = SystemCreatedBy
        };

        db.Users.Add(entity);
        // If SaveChanges fails (DB down, unique constraint race, etc.), no catch here: exception flows to the controller.
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (entity.ToPublicDto(), null);
    }

    public async Task<(UserPublicDto user, string? errorCode)> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.IsActive)
            return (default!, "INVALID_CREDENTIALS");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return (default!, "INVALID_CREDENTIALS");

        // DB errors from ExecuteUpdateAsync will bubble to the controller.
        await db.Users
            .Where(u => u.IdUser == user.IdUser)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.LastLoginAt, _ => DateTime.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);

        user.LastLoginAt = DateTime.UtcNow;
        return (user.ToPublicDto(), null);
    }
}
