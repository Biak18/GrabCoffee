using GrabCoffee.Application.Abstractions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GrabCoffee.Infrastructure.Authentication;

public sealed class SupabaseAuthClient(HttpClient http) : IAuthClient
{
    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        var res = await http.PostAsJsonAsync("token?grant_type=password",
            new { email, password }, ct);
        if (!res.IsSuccessStatusCode)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var body = await res.Content.ReadFromJsonAsync<TokenResponse>(ct)
            ?? throw new InvalidOperationException("Invalid auth response.");
        return new AuthResult(body.AccessToken, body.RefreshToken, body.ExpiresIn);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var res = await http.PostAsJsonAsync("token?grant_type=refresh_token",
            new { refresh_token = refreshToken }, ct);
        if (!res.IsSuccessStatusCode)
            throw new UnauthorizedAccessException("Session expired. Please sign in again.");

        var body = await res.Content.ReadFromJsonAsync<TokenResponse>(ct)
            ?? throw new InvalidOperationException("Invalid auth response.");
        return new AuthResult(body.AccessToken, body.RefreshToken, body.ExpiresIn);
    }

    public async Task<RegisterResult> RegisterAsync(string email, string password, string? fullName, CancellationToken ct)
    {
        var res = await http.PostAsJsonAsync("signup",
            new { email, password, data = new { full_name = fullName } }, ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException("Registration failed.");

        var body = await res.Content.ReadFromJsonAsync<TokenResponse>(ct);
        var confirmationRequired = body is null || string.IsNullOrEmpty(body.AccessToken);
        return new RegisterResult(body?.AccessToken, body?.RefreshToken, body?.ExpiresIn ?? 0, confirmationRequired);
    }

    public async Task LogoutAsync(string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "logout");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var res = await http.SendAsync(req, ct);
        // Idempotent: 401/403/404 from GoTrue = already logged out.
        if (res.IsSuccessStatusCode
            || res.StatusCode == System.Net.HttpStatusCode.Unauthorized
            || res.StatusCode == System.Net.HttpStatusCode.Forbidden
            || res.StatusCode == System.Net.HttpStatusCode.NotFound)
            return;
        throw new InvalidOperationException("Logout failed.");
    }

    public Task RequestPasswordResetAsync(string email, CancellationToken ct)
    {
        // Fire-and-forget per pattern: never throw, never reveal if email exists.
        // GoTrue returns 200 even for unknown emails; we swallow all failures.
        return TryRecoverAsync(email, ct);
    }

    // Mirrors supabase.auth.updateUser: caller's own Bearer token authorizes it.
    public async Task UpdatePasswordAsync(string accessToken, string newPassword, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, "user");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(new { password = newPassword });
        var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException("Password change failed.");
    }

    private async Task TryRecoverAsync(string email, CancellationToken ct)
    {
        try
        {
            await http.PostAsJsonAsync("recover", new { email }, ct);
        }
        catch
        {
            // Intentionally silent.
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }
}
