namespace HongAnhShop.Models;

public sealed class AdminStaffUserRow
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AdminStaffPage
{
    public List<AdminStaffUserRow> Accounts { get; set; } = new();
    public string Query { get; set; } = "";
    public string Filter { get; set; } = "all";
    public int TotalUsers { get; set; }
    public int TotalStaff { get; set; }
}

public enum AdminStaffChangeResult
{
    Updated,
    NotFound,
    AlreadyChanged,
    InactiveAccount,
    InvalidTransition
}
