using GrabCoffee.Application.Features.Addresses.CreateAddress;
using GrabCoffee.Application.Features.Addresses.DeleteAddress;
using GrabCoffee.Application.Features.Addresses.GetAddresses;
using GrabCoffee.Application.Features.Addresses.SetDefaultAddress;
using GrabCoffee.Application.Features.Addresses.UpdateAddress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("addresses")]
public sealed class AddressesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await sender.Send(new GetAddressesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] AddressInputRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateAddressCommand(
            request.Label, request.FullName, request.Phone, request.Address,
            request.Lat, request.Lng, request.IsDefault), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AddressInputRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateAddressCommand(
            id, request.Label, request.FullName, request.Phone, request.Address,
            request.Lat, request.Lng), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteAddressCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/default")]
    [Authorize]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        await sender.Send(new SetDefaultAddressCommand(id), ct);
        return NoContent();
    }

    public sealed record AddressInputRequest(
        string Label,
        string FullName,
        string Phone,
        string Address,
        double? Lat,
        double? Lng,
        bool IsDefault);
}
