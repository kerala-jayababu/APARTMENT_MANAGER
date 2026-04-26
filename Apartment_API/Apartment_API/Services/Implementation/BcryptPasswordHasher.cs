using Apartment_API.Services.Interfaces;

namespace Apartment_API.Services.Implementation;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainPassword) => BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: WorkFactor);

    public bool Verify(string plainPassword, string passwordHash) =>
        !string.IsNullOrEmpty(passwordHash) && BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
}
