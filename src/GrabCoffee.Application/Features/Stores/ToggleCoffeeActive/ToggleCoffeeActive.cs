using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Application.Features.Coffees;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.ToggleCoffeeActive;

// Mirrors toggleCoffeeActive.
public sealed record ToggleCoffeeActiveCommand(Guid Id, bool IsActive) : IRequest<CoffeeDto>;

public sealed class ToggleCoffeeActiveValidator : AbstractValidator<ToggleCoffeeActiveCommand>
{
    public ToggleCoffeeActiveValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class ToggleCoffeeActiveHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ToggleCoffeeActiveCommand, CoffeeDto>
{
    public async Task<CoffeeDto> Handle(ToggleCoffeeActiveCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var coffee = await db.FindCoffeeAsync(request.Id, ct);
        if (coffee is null || coffee.StoreId != store.Id)
            throw new NotFoundException($"Coffee {request.Id} not found.");

        coffee.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return CoffeeMapper.ToDto(coffee, store.Name);
    }
}
