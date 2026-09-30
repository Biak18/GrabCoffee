using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Application.Features.Coffees;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.UpdateCoffee;

// Mirrors updateCoffee(id, partial): all fields optional, only supplied
// values change. Never touches another seller's coffee (404 either way).
public sealed record UpdateCoffeeCommand(
    Guid Id,
    string? Name,
    string? Description,
    decimal? BasePrice,
    string? ImageUrl,
    Guid? CategoryId,
    bool? IsFeatured,
    bool? IsActive) : IRequest<CoffeeDto>;

public sealed class UpdateCoffeeValidator : AbstractValidator<UpdateCoffeeCommand>
{
    public UpdateCoffeeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BasePrice).GreaterThan(0).When(x => x.BasePrice.HasValue);
        RuleFor(x => x.ImageUrl).MaximumLength(1000);
    }
}

public sealed class UpdateCoffeeHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateCoffeeCommand, CoffeeDto>
{
    public async Task<CoffeeDto> Handle(UpdateCoffeeCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var coffee = await db.FindCoffeeAsync(request.Id, ct);
        if (coffee is null || coffee.StoreId != store.Id)
            throw new NotFoundException($"Coffee {request.Id} not found.");

        if (request.CategoryId.HasValue)
        {
            var category = await db.FindCategoryAsync(request.CategoryId.Value, ct);
            if (category is null || category.StoreId != store.Id)
                throw new InvalidOperationException("Category does not belong to your store.");
            coffee.CategoryId = request.CategoryId;
        }

        if (request.Name is not null)
            coffee.Name = request.Name.Trim();
        if (request.Description is not null)
            coffee.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.BasePrice.HasValue)
            coffee.BasePrice = request.BasePrice.Value;
        if (request.ImageUrl is not null)
            coffee.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        if (request.IsFeatured.HasValue)
            coffee.IsFeatured = request.IsFeatured.Value;
        if (request.IsActive.HasValue)
            coffee.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync(ct);
        return CoffeeMapper.ToDto(coffee, store.Name);
    }
}
