namespace HongAnhShop.Models;

public sealed class AdminCustomerRow
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}

public sealed class AdminCustomersPage
{
    public List<AdminCustomerRow> Customers { get; set; } = new();
    public string Query { get; set; } = "";
    public string Status { get; set; } = "all";
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
}

public sealed class CustomerSecurityState
{
    public bool IsActive { get; set; }
    public string Role { get; set; } = "";
}
