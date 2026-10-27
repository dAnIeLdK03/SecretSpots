using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;

namespace SecretSpots.Features.Common.Persistence;

public static class UserQueryExtensions
{
    // Empty rather than null when the user is gone (e.g. deleted mid-request), so responses that
    // show a creator's name never have to deal with a missing one.
    public static async Task<string> GetDisplayNameAsync(this IQueryable<User> users, Guid userId, CancellationToken cancellationToken) =>
        await users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;
}
