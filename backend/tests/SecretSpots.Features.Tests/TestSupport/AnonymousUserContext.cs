using SecretSpots.Domain;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Tests.TestSupport;

// Same shape as the real context for an unauthenticated request: UserIdOrNull is null and the
// throwing members throw, so a handler that forgets to check UserIdOrNull first fails the test.
internal class AnonymousUserContext : IUserContext
{
    public Guid UserId => throw new InvalidOperationException("No authenticated user.");

    public Guid? UserIdOrNull => null;

    public UserRole Role => throw new InvalidOperationException("No authenticated user.");
}
