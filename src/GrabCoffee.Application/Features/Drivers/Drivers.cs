using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Drivers;

public sealed record DriverDto(
    Guid Id,
    string? FullName,
    string? Phone,
    string? Vehicle,
    bool IsAvailable);

public sealed record RegisterDriverCommand(
    string? FullName,
    string? Phone,
    string? Vehicle) : IRequest;

public sealed record SetAvailabilityCommand(bool IsAvailable) : IRequest;

public sealed record GetMyDriverQuery : IRequest<DriverDto?>;

public sealed record GetAvailableDriversQuery : IRequest<IReadOnlyList<DriverDto>>;

public sealed class RegisterDriverValidator : AbstractValidator<RegisterDriverCommand>
{
    public RegisterDriverValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Vehicle).MaximumLength(200);
    }
}

// Port of register_driver(): upserts the driver row (nulls keep old values,
// always available) and flips role to driver through the role lock.
public sealed class RegisterDriverHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<RegisterDriverCommand>
{
    public async Task Handle(RegisterDriverCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        await db.RegisterDriverAsync(
            userId,
            string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            string.IsNullOrWhiteSpace(request.Vehicle) ? null : request.Vehicle.Trim(),
            ct);
    }
}

public sealed class GetMyDriverHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyDriverQuery, DriverDto?>
{
    public async Task<DriverDto?> Handle(GetMyDriverQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var driver = await db.FindDriverAsync(userId, ct);
        return driver is null
            ? null
            : new DriverDto(driver.Id, driver.FullName, driver.Phone, driver.Vehicle, driver.IsAvailable);
    }
}

public sealed class SetAvailabilityHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SetAvailabilityCommand>
{
    public async Task Handle(SetAvailabilityCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        await db.SetDriverAvailabilityAsync(userId, request.IsAvailable, ct);
    }
}

public sealed class GetAvailableDriversHandler(IAppDbContext db)
    : IRequestHandler<GetAvailableDriversQuery, IReadOnlyList<DriverDto>>
{
    public async Task<IReadOnlyList<DriverDto>> Handle(GetAvailableDriversQuery request, CancellationToken ct)
    {
        var drivers = await db.GetAvailableDriversAsync(ct);
        return drivers.Select(d => new DriverDto(d.Id, d.FullName, d.Phone, d.Vehicle, d.IsAvailable)).ToList();
    }
}
