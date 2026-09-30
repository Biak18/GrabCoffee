using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Coffees;
using GrabCoffee.Application.Features.Stores;
using MediatR;

namespace GrabCoffee.Application.Features.Search;

// Mirrors searchCoffees + searchStores (min 2 chars, ilike on name/description
// and name/address). One round trip instead of two.
public sealed record SearchQuery(string Term) : IRequest<SearchResultDto>;

public sealed record SearchResultDto(
    IReadOnlyList<CoffeeDto> Coffees,
    IReadOnlyList<StoreDto> Stores);

public sealed class SearchValidator : AbstractValidator<SearchQuery>
{
    public SearchValidator()
    {
        RuleFor(x => x.Term).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}

public sealed class SearchHandler(IAppDbContext db) : IRequestHandler<SearchQuery, SearchResultDto>
{
    public async Task<SearchResultDto> Handle(SearchQuery request, CancellationToken ct)
    {
        var term = request.Term.Trim();
        var coffees = await db.SearchCoffeesAsync(term, ct);
        var stores = await db.SearchStoresAsync(term, ct);

        var names = await db.GetStoreNamesAsync(coffees.Select(c => c.StoreId).Distinct(), ct);

        return new SearchResultDto(
            coffees.Select(c => CoffeeMapper.ToDto(c, names.GetValueOrDefault(c.StoreId))).ToList(),
            stores.Select(StoreMapper.ToDto).ToList());
    }
}
