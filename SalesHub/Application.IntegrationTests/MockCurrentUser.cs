using Application.Interfaces.Security;

namespace Application.IntegrationTests;

public class MockCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
}
