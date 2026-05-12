namespace SubscriptionApp.Api.Dtos.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public AuthUserInfo User { get; set; } = null!;
}

public class AuthUserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
