using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Tests.TestSupport;

internal class FakeUserContext(Guid userId) : IUserContext
{
    public Guid UserId => userId;
}
