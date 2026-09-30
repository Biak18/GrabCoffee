using System.Reflection;
using FluentValidation;
using GrabCoffee.Application.Authorization;
using GrabCoffee.Application.Common.Behaviors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GrabCoffee.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        // Admin gate: [Authorize(Policy = "Admin")] -> seller role in profiles
        services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();

        return services;
    }
}
