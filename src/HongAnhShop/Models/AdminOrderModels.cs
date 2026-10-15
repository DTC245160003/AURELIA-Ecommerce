namespace HongAnhShop.Models;

public sealed class AdminOrder
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerAddress { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
}

public sealed class AdminOrderLine
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class AdminOrderStats
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public decimal CompletedValue { get; set; }
}

public sealed class AdminOrdersPageVm
{
    public List<AdminOrder> Orders { get; set; } = new();
    public AdminOrderStats Stats { get; set; } = new();
    public string Search { get; set; } = "";
    public string StatusFilter { get; set; } = "";
}

public sealed class AdminOrderDetailsVm
{
    public AdminOrder Order { get; set; } = new();
    public List<AdminOrderLine> Lines { get; set; } = new();
}

public enum AdminOrderStatusResult
{
    Updated,
    NotFound,
    InvalidTransition,
    AlreadySet
}
