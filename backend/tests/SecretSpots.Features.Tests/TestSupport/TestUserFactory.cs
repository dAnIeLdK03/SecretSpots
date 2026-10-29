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
        db.CrystalWallets.Add(new CrystalWallet { UserId = user.Id, Balance = 0 });
        await db.SaveChangesAsync();

        return user;
    }

    // For tests that only need "a user" (or an admin) and never log in — skips BCrypt, which is
    // deliberately slow, so seeding dozens of these stays cheap. No password (external-login shape).
    public static async Task<User> SeedAsync(IAppDbContext db, UserRole role = UserRole.User, string displayName = "Seeded User")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"user-{Guid.NewGuid():N}@example.com",
            DisplayName = displayName,
            Role = role,
        };

        db.Users.Add(user);
        db.CrystalWallets.Add(new CrystalWallet { UserId = user.Id, Balance = 0 });
        await db.SaveChangesAsync();

        return user;
    }
}
