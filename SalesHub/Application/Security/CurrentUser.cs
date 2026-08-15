using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Security;

public class CurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var value = User?.FindFirstValue("user_id");

            if (Guid.TryParse(value, out var id))
            {
                return id;
            }

            return Guid.Empty;
        }
    }

    public string? Username
    {
        get
        {
            var value = User?.FindFirstValue("username");

            return value;
        }
    }

}
