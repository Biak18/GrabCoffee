using FluentValidation;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized."),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden."),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found."),
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Request failed."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        // Never leak "email exists" — InvalidOperationException from Register stays generic.

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception is ValidationException vex
                ? string.Join("; ", vex.Errors.Select(e => e.ErrorMessage))
                : exception.Message,
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
