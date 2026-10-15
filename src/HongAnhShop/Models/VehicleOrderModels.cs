namespace HongAnhShop.Models;

public sealed class VehicleOrderSummary
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = "";
    public string Status { get; set; } = "pending";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalVehicles { get; set; }
}

public sealed class VehicleOrderItem
{
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class VehicleOrderDetails
{
    public VehicleOrderSummary Order { get; set; } = new();
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerAddress { get; set; } = "";
    public List<VehicleOrderItem> Items { get; set; } = new();
}
