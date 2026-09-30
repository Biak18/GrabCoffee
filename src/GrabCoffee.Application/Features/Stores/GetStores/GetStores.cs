using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Models;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.GetStores;

public sealed record GetStoresQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<StoreDto>>;

public sealed class GetStoresValidator : AbstractValidator<GetStoresQuery>
{
    public GetStoresValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetStoresHandler(IAppDbContext db) : IRequestHandler<GetStoresQuery, PagedResult<StoreDto>>
{
    public async Task<PagedResult<StoreDto>> Handle(GetStoresQuery request, CancellationToken ct)
    {
        var stores = await db.GetStoresAsync(request.Page, request.PageSize, ct);
        var total = await db.CountStoresAsync(ct);
        return new PagedResult<StoreDto>(
            stores.Select(StoreMapper.ToDto).ToList(),
            request.Page,
            request.PageSize,
            total);
    }
}
