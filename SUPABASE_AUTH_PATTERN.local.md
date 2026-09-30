# Supabase auth pattern (personal reference — DressShop style)

Reusable recipe: Supabase Auth (GoTrue) as the identity provider for any
.NET Clean-Architecture backend. The backend never stores passwords and
needs no service-role key — only the anon key plus a `profiles` table for
roles. Copied from DressShop (`D:\DressShop`), which uses exactly this.

## 1. The idea in one paragraph

- Users exist in Supabase Auth. The backend exchanges email+password for
  Supabase tokens (`IAuthClient`), validates the Supabase JWT on every
  request (Authority = `{Supabase:Url}/auth/v1`), and reads the user id
  from the `sub` claim. Roles live in your own `profiles` table
  (`id` = auth user id, `role` = `admin`/`leader`), never in client claims.
- Register, refresh, logout and password-reset are thin MediatR commands
  that call GoTrue over HTTP. Login never touches your database.

## 2. Secrets (user-secrets or env)

```powershell
dotnet user-secrets set "Supabase:Url" "https://<ref>.supabase.co"
dotnet user-secrets set "Supabase:AnonKey" "<anon key>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<session-pooler connection string>"
```

No service-role key. Ever, for auth.

## 3. Program.cs — JWT validation + policies

```csharp
var supabaseUrl = builder.Configuration["Supabase:Url"]?.TrimEnd('/')
    ?? throw new InvalidOperationException("Supabase:Url is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{supabaseUrl}/auth/v1";
        options.Audience = "authenticated";
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AdminRequirement());
    }));
builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
```

`MapInboundClaims = false` keeps the `sub` claim readable as `"sub"`.

## 4. `IAuthClient` (Application/Abstractions) — the contract

```csharp
public interface IAuthClient
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct);
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct);
    Task<RegisterResult> RegisterAsync(string email, string password, string? fullName, CancellationToken ct);
    Task LogoutAsync(string accessToken, CancellationToken ct);          // idempotent
    Task RequestPasswordResetAsync(string email, CancellationToken ct);  // always silent
}

public sealed record AuthResult(string AccessToken, string RefreshToken, int ExpiresIn);
public sealed record RegisterResult(string? AccessToken, string? RefreshToken, int ExpiresIn, bool EmailConfirmationRequired);
```

## 5. `SupabaseAuthClient` (Infrastructure/Authentication) — the 5 GoTrue calls

One `HttpClient`, `BaseAddress = {url}/auth/v1/`, default header
`apikey: <anon key>`. All bodies are JSON:

| Method | Call | Success | Failure mapping |
|---|---|---|---|
| Login | `POST token?grant_type=password { email, password }` | tokens | non-2xx → `UnauthorizedAccessException("Invalid email or password.")` (401, never reveal which field was wrong) |
| Refresh | `POST token?grant_type=refresh_token { refresh_token }` | tokens | non-2xx → `UnauthorizedAccessException` (client must re-login) |
| Register | `POST signup { email, password, data: { full_name } }` | tokens, **or empty access_token when email confirmation is ON** → return `RegisterResult(..., EmailConfirmationRequired: true)` so the UI can say "check your inbox" | non-2xx → `InvalidOperationException` (likely duplicate email → 400, still no enumeration) |
| Logout | `POST logout` with `Authorization: Bearer <token>` | — | 401/403/404 from GoTrue = already logged out → swallow; anything else → throw |
| Reset | `POST recover { email }` | always 204 | never throw — GoTrue returns 200 even for unknown emails |

Response DTOs use `[JsonPropertyName("access_token")]` etc.

## 6. Application layer — thin commands

One folder per operation (`Features/Auth/Login|Refresh|Register|Logout|
ForgotPassword`), each a 3-liner: record + FluentValidation
(`EmailAddress`, password `MinimumLength(6)`) + handler delegating to
`IAuthClient`. No business logic lives here — that is the point.

## 7. `AuthController` — routes

```text
POST auth/login           AllowAnonymous  { email, password }
POST auth/refresh         AllowAnonymous  { refreshToken }
POST auth/register        AllowAnonymous  { email, password, fullName? }   (omit if no public signup)
POST auth/logout          Authorize       (Bearer token read from the Authorization header)
POST auth/forgot-password AllowAnonymous  { email } -> always 204
GET  auth/me              Authorize       -> { id, email, displayName, role } (role from profiles table)
```

Whole controller under a strict rate-limit policy (`auth`: 5/min/IP).
Logout extracts the token with
`authorization["Bearer ".Length..].Trim()` after a `StartsWith` check.

## 8. `ICurrentUser` + Admin gate

```csharp
UserId => Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;
Email  => User.FindFirstValue("email");
```

`AdminAuthorizationHandler`: if `UserId` is null do nothing; else
`profiles.Any(p => p.Id == userId && p.Role == "admin")` → Succeed.
Use `[Authorize(Policy = "Admin")]` for user/role/settings management.
Ownership (`created_by == UserId`, admins bypass) is enforced inside
write handlers, not in controllers.

## 9. Error mapping (GlobalExceptionHandler)

`UnauthorizedAccessException` → 401, FluentValidation → 400,
missing → 404, ownership violations → 403. Never leak "email exists".

## 10. Frontend pairing (React)

- `signIn`: POST login → store both tokens → GET me.
- Page load: stored refresh token → POST refresh once → store pair → GET
  me; 401 → clear → `/login`.
- `signOut`: best-effort POST logout → clear storage.
- If the frontend also uses `supabase-js` directly (storage/uploads),
  hydrate it after every login/refresh:
  `supabase.auth.setSession({ access_token, refresh_token })`.
- If all traffic goes through the backend, drop `supabase-js` entirely.

## 11. Reuse checklist for the next project

1. Copy: `IAuthClient`, `SupabaseAuthClient`, `CurrentUser`,
   `AdminRequirement/Handler`, `Features/Auth/*`, `AuthController`,
   the Program.cs block, exception mappings.
2. Set the 3 secrets (§2). Create `profiles(id uuid PK, role text default
   'leader')` with `id` = auth user id (FK to `auth.users` optional).
3. Decide: public `auth/register` or not. If not, delete it (CityYouth
   has no register — admins create users in the Supabase dashboard).
4. Keep password rules consistent with the Supabase dashboard
   (Auth → Password protection) so client and server agree.
5. Supabase dashboard → Auth → URL Configuration: set Site URL + redirect
   URLs, otherwise confirmation/recovery emails link to localhost.

## 12. Gotchas learned

- Refresh tokens rotate: always store the **new** refresh token from every
  refresh response; reusing the old one 401s.
- `sub` claim is the auth user id (GUID). If `UserId` is always null, you
  forgot `MapInboundClaims = false` or the token isn't Supabase-issued.
- Email-confirmation ON means register returns **no session** — handle
  `EmailConfirmationRequired`, don't treat it as an error.
- Logout must be idempotent — expired tokens are already "logged out".
- The anon key in the backend is fine (it's public by design); the key
  that must never ship anywhere is the service-role key, which this
  pattern never uses.
