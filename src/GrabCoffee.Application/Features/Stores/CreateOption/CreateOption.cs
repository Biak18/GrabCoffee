using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Domain.Entities;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.CreateOption;

// Mirrors createOption. Type: size | temperature | milk | extra.
public sealed record CreateOptionCommand(string Type, string Label, decimal PriceDelta)
    : IRequest<SellerOptionDto>;

public sealed class CreateOptionValidator : AbstractValidator<CreateOptionCommand>
{
    private static readonly string[] AllowedTypes = ["size", "temperature", "milk", "extra"];

    public CreateOptionValidator()
    {
        RuleFor(x => x.Type)
            .Must(t => AllowedTypes.Contains(t.ToLowerInvariant()))
            .WithMessage("Type must be one of: size, temperature, milk, extra.");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PriceDelta).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateOptionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateOptionCommand, SellerOptionDto>
{
    public async Task<SellerOptionDto> Handle(CreateOptionCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var option = new CoffeeOption
        {
            Id = Guid.NewGuid(),
            Type = request.Type.Trim().ToLowerInvariant(),
            Label = request.Label.Trim(),
            PriceDelta = request.PriceDelta,
            StoreId = store.Id,
        };
        await db.AddOptionAsync(option, ct);
        await db.SaveChangesAsync(ct);
        return new SellerOptionDto(option.Id, option.Type, option.Label, option.PriceDelta, option.StoreId, []);
    }
}
