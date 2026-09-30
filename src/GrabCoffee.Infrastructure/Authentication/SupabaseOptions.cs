namespace GrabCoffee.Infrastructure.Authentication;

public sealed class SupabaseOptions
{
    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
}
