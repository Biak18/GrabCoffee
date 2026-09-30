using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Coffees.GetCoffeeById;

public sealed record GetCoffeeByIdQuery(Guid Id) : IRequest<CoffeeDto>;

public sealed class GetCoffeeByIdHandler(IAppDbContext db) : IRequestHandler<GetCoffeeByIdQuery, CoffeeDto>
{
    public async Task<CoffeeDto> Handle(GetCoffeeByIdQuery request, CancellationToken ct)
    {
        var coffee = await db.FindCoffeeAsync(request.Id, ct)
            ?? throw new NotFoundException($"Coffee {request.Id} not found.");
        var names = await db.GetStoreNamesAsync([coffee.StoreId], ct);
        return CoffeeMapper.ToDto(coffee, names.GetValueOrDefault(coffee.StoreId));
    }
}
