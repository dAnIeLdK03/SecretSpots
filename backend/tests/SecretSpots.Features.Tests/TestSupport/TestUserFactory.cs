using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Tests.TestSupport;

internal static class TestUserFactory
{
    public static async Task<User> SeedAsync(IAppDbContext db, string email, string rawPassword)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            DisplayName = "Seeded User",
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }
}
