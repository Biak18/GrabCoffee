using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Categories;
using GrabCoffee.Domain.Entities;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.CreateCategory;

// Mirrors createCategory (sort_order starts at 0).
public sealed record CreateCategoryCommand(string Name) : IRequest<CategoryDto>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateCategoryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            SortOrder = 0,
            StoreId = store.Id,
        };
        await db.AddCategoryAsync(category, ct);
        await db.SaveChangesAsync(ct);
        return CategoryMapper.ToDto(category);
    }
}
