using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Coffees;
using GrabCoffee.Domain.Entities;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.CreateCoffee;

// Mirrors createCoffee. CategoryId must belong to the seller's own store.
public sealed record CreateCoffeeCommand(
    string Name,
    string? Description,
    decimal BasePrice,
    string? ImageUrl,
    Guid? CategoryId,
    bool IsFeatured,
    bool IsActive) : IRequest<CoffeeDto>;

public sealed class CreateCoffeeValidator : AbstractValidator<CreateCoffeeCommand>
{
    public CreateCoffeeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BasePrice).GreaterThan(0);
        RuleFor(x => x.ImageUrl).MaximumLength(1000);
    }
}

public sealed class CreateCoffeeHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateCoffeeCommand, CoffeeDto>
{
    public async Task<CoffeeDto> Handle(CreateCoffeeCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        if (request.CategoryId.HasValue)
        {
            var category = await db.FindCategoryAsync(request.CategoryId.Value, ct);
            if (category is null || category.StoreId != store.Id)
                throw new InvalidOperationException("Category does not belong to your store.");
        }

        var coffee = new Coffee
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            BasePrice = request.BasePrice,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            CategoryId = request.CategoryId,
            IsFeatured = request.IsFeatured,
            IsActive = request.IsActive,
            StoreId = store.Id,
        };
        await db.AddCoffeeAsync(coffee, ct);
        await db.SaveChangesAsync(ct);
        return CoffeeMapper.ToDto(coffee, store.Name);
    }
}
