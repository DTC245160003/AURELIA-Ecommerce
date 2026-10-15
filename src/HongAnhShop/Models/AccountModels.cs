namespace HongAnhShop.Models;

public sealed class RegisterInput
{
    public string FullName { get; set; } = "";
    public string EmailOrPhone { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public bool AgreeTerms { get; set; }
}

public sealed class LoginInput
{
    public string EmailOrPhone { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

public sealed class ShopUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}
