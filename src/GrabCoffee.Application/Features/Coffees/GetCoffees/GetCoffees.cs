using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Models;
using MediatR;

namespace GrabCoffee.Application.Features.Coffees.GetCoffees;

// Mirrors fetchMenuCoffees: store + category + search + sort + paging.
// Sort: popular (rating desc) | price_asc | price_desc | name.
public sealed record GetCoffeesQuery(
    Guid? StoreId = null,
    Guid? CategoryId = null,
    string? Search = null,
    string Sort = "popular",
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CoffeeDto>>;

public sealed class GetCoffeesValidator : AbstractValidator<GetCoffeesQuery>
{
    private static readonly string[] AllowedSorts = ["popular", "price_asc", "price_desc", "name"];

    public GetCoffeesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Sort)
            .Must(s => AllowedSorts.Contains(s.ToLowerInvariant()))
            .WithMessage("Sort must be one of: popular, price_asc, price_desc, name.");
    }
}

public sealed class GetCoffeesHandler(IAppDbContext db) : IRequestHandler<GetCoffeesQuery, PagedResult<CoffeeDto>>
{
    public async Task<PagedResult<CoffeeDto>> Handle(GetCoffeesQuery request, CancellationToken ct)
    {
        var coffees = await db.GetCoffeesAsync(
            request.StoreId, request.CategoryId, request.Search,
            request.Sort, request.Page, request.PageSize, onlyActive: true, ct);
        var total = await db.CountCoffeesAsync(
            request.StoreId, request.CategoryId, request.Search, onlyActive: true, ct);
        var dtos = await CoffeeMapper.ToDtosAsync(db, coffees, ct);
        return new PagedResult<CoffeeDto>(dtos, request.Page, request.PageSize, total);
    }
}
