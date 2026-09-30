using GrabCoffee.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GrabCoffee.Infrastructure.Authentication;

public sealed class CurrentUser(IHttpContextAccessor accesser) : ICurrentUser
{
    private ClaimsPrincipal? User => accesser.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue("email");
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
