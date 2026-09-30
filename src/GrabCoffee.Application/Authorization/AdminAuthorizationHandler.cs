using GrabCoffee.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace GrabCoffee.Application.Authorization;

public sealed class AdminAuthorizationHandler(IAppDbContext db, ICurrentUser currentUser) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (currentUser.UserId is not { } userId)
            return;

        var isAdmin = await db.IsAdminAsync(userId, CancellationToken.None);
        if (isAdmin)
            context.Succeed(requirement);
    }
}
