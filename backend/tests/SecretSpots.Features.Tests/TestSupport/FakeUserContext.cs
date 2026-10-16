using SecretSpots.Domain;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Tests.TestSupport;

internal class FakeUserContext(Guid userId, UserRole role = UserRole.User) : IUserContext
{
    public Guid UserId => userId;

    public Guid? UserIdOrNull => userId;

    public UserRole Role => role;
}
