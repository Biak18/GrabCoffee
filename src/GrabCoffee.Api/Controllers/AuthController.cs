using GrabCoffee.Application.Features.Auth.ChangePassword;
using GrabCoffee.Application.Features.Auth.ForgotPassword;
using GrabCoffee.Application.Features.Auth.Login;
using GrabCoffee.Application.Features.Auth.Logout;
using GrabCoffee.Application.Features.Auth.Me;
using GrabCoffee.Application.Features.Auth.Refresh;
using GrabCoffee.Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new RefreshCommand(request.RefreshToken), ct);
        return Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new RegisterCommand(request.Email, request.Password, request.FullName), ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization["Bearer ".Length..].Trim();
            if (!string.IsNullOrEmpty(token))
                await sender.Send(new LogoutCommand(token), ct);
        }
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email), ct);
        return NoContent(); // always silent
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized();
        var token = authorization["Bearer ".Length..].Trim();

        await sender.Send(new ChangePasswordCommand(token, request.NewPassword), ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await sender.Send(new MeQuery(), ct);
        return Ok(result);
    }

    public sealed record LoginRequest
    {
        /// <example>biakceu912@gmail.com</example>
        public required string Email { get; init; }

        /// <example>Biak18*</example>
        public required string Password { get; init; }
    }
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record RegisterRequest(string Email, string Password, string? FullName);
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ChangePasswordRequest(string NewPassword);
}
