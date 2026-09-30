namespace GrabCoffee.Application.Abstractions;

public interface IAuthClient
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct);
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct);
    Task<RegisterResult> RegisterAsync(string email, string password, string? fullName, CancellationToken ct);
    Task LogoutAsync(string accessToken, CancellationToken ct);
    Task RequestPasswordResetAsync(string email, CancellationToken ct);
    Task UpdatePasswordAsync(string accessToken, string newPassword, CancellationToken ct);
}

public sealed record AuthResult(string AccessToken, string RefreshToken, int ExpiresIn);
public sealed record RegisterResult(string? AccessToken, string? RefreshToken, int ExpiresIn, bool EmailConfirmationRequired);
