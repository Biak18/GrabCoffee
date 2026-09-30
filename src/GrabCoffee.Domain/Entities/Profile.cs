namespace GrabCoffee.Domain.Entities;

// Mirrors public.profiles(id uuid PK FK auth.users, full_name, avatar_url, role, created_at).
// id == Supabase auth.users.id (sub claim). Role constraint: seller | customer | driver.
// seller == store owner / admin equivalent for [Authorize(Policy = "Admin")].
public sealed class Profile
{
    public Guid Id { get; set; }
    public string Role { get; set; } = "customer";
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsSeller =>
        string.Equals(Role, "seller", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);
}
