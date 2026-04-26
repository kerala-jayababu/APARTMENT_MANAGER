using Apartment_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
}
