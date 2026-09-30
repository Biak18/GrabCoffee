using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.UpdateOption;

// Mirrors updateOption(id, partial).
public sealed record UpdateOptionCommand(
    Guid Id,
    string? Type,
    string? Label,
    decimal? PriceDelta) : IRequest<SellerOptionDto>;

public sealed class UpdateOptionValidator : AbstractValidator<UpdateOptionCommand>
{
    private static readonly string[] AllowedTypes = ["size", "temperature", "milk", "extra"];

    public UpdateOptionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Type)
            .Must(t => t is null || AllowedTypes.Contains(t.ToLowerInvariant()))
            .WithMessage("Type must be one of: size, temperature, milk, extra.");
        RuleFor(x => x.Label).MaximumLength(200);
        RuleFor(x => x.PriceDelta).GreaterThanOrEqualTo(0).When(x => x.PriceDelta.HasValue);
    }
}

public sealed class UpdateOptionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateOptionCommand, SellerOptionDto>
{
    public async Task<SellerOptionDto> Handle(UpdateOptionCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var option = await db.FindOptionAsync(request.Id, ct);
        if (option is null || option.StoreId != store.Id)
            throw new NotFoundException($"Option {request.Id} not found.");

        if (request.Type is not null)
            option.Type = request.Type.Trim().ToLowerInvariant();
        if (request.Label is not null)
            option.Label = request.Label.Trim();
        if (request.PriceDelta.HasValue)
            option.PriceDelta = request.PriceDelta.Value;

        await db.SaveChangesAsync(ct);

        var scoping = await db.GetOptionCategoryMapAsync(store.Id, ct);
        return new SellerOptionDto(
            option.Id, option.Type, option.Label, option.PriceDelta, option.StoreId,
            scoping.GetValueOrDefault(option.Id, []));
    }
}
