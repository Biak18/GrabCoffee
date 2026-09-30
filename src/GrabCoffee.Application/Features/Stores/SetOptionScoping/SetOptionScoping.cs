using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.SetOptionScoping;

// Mirrors set_option_category_scoping(): replace-all scoping; empty list
// means unscoped (option applies to every category). Every category must
// belong to the seller's own store.
public sealed record SetOptionScopingCommand(Guid OptionId, List<Guid> CategoryIds) : IRequest;

public sealed class SetOptionScopingValidator : AbstractValidator<SetOptionScopingCommand>
{
    public SetOptionScopingValidator()
    {
        RuleFor(x => x.OptionId).NotEmpty();
    }
}

public sealed class SetOptionScopingHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SetOptionScopingCommand>
{
    public async Task Handle(SetOptionScopingCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var option = await db.FindOptionAsync(request.OptionId, ct);
        if (option is null || option.StoreId != store.Id)
            throw new NotFoundException($"Option {request.OptionId} not found.");

        foreach (var categoryId in request.CategoryIds.Distinct())
        {
            var category = await db.FindCategoryAsync(categoryId, ct);
            if (category is null || category.StoreId != store.Id)
                throw new InvalidOperationException("All categories must belong to your store.");
        }

        await db.SetOptionScopingAsync(request.OptionId, request.CategoryIds, ct);
        await db.SaveChangesAsync(ct);
    }
}
