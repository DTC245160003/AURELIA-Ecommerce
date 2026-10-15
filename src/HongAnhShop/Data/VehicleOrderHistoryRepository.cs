using HongAnhShop.Models;
using MySqlConnector;

namespace HongAnhShop.Data;

/// <summary>Orders linked to a verified user ID. Never match orders by display name/phone.</summary>
public sealed class VehicleOrderHistoryRepository
{
    private readonly IConfiguration _config;
    public VehicleOrderHistoryRepository(IConfiguration config) => _config = config;

    private MySqlConnection OpenConnection()
    {
        var password = _config["MYSQL_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Chưa cấu hình MYSQL_PASSWORD.");
        return new MySqlConnection(new MySqlConnectionStringBuilder
        {
            Server = _config["MYSQL_HOST"] ?? "127.0.0.1",
            Port = uint.TryParse(_config["MYSQL_PORT"], out var port) ? port : 3306,
            Database = _config["MYSQL_DATABASE"] ?? "honganhshop",
            UserID = _config["MYSQL_USER"] ?? "shop_app",
            Password = password,
            CharacterSet = "utf8mb4"
        }.ConnectionString);
    }

    private static VehicleOrderSummary ReadSummary(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        OrderCode = reader.GetString(1),
        Status = reader.GetString(2),
        TotalAmount = reader.GetDecimal(3),
        CreatedAt = reader.GetDateTime(4),
        TotalVehicles = Convert.ToInt32(reader.GetValue(5))
    };

    /// <summary>Also used by Admin/Staff after their existing customer-role check.</summary>
    public async Task<List<VehicleOrderSummary>> GetByUserIdAsync(int userId, int limit = 100)
    {
        var orders = new List<VehicleOrderSummary>();
        if (userId <= 0) return orders;
        limit = Math.Clamp(limit, 1, 100);

        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand("""
            SELECT o.id, o.order_code, o.status, o.total_amount, o.created_at,
                   COALESCE((SELECT SUM(oi.quantity) FROM order_items oi
                              WHERE oi.order_id = o.id), 0) AS vehicle_count
              FROM orders o
             WHERE o.user_id = @userId
             ORDER BY o.created_at DESC, o.id DESC
             LIMIT @limit
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@limit", limit);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) orders.Add(ReadSummary(reader));
        return orders;
    }

    /// <summary>Ownership enforced in SQL, preventing IDOR on order details.</summary>
    public async Task<VehicleOrderDetails?> GetForOwnerAsync(int orderId, int userId)
    {
        if (orderId <= 0 || userId <= 0) return null;
        await using var connection = OpenConnection();
        await connection.OpenAsync();

        VehicleOrderDetails? result = null;
        await using (var command = new MySqlCommand("""
            SELECT o.id, o.order_code, o.status, o.total_amount, o.created_at,
                   COALESCE((SELECT SUM(oi.quantity) FROM order_items oi
                              WHERE oi.order_id = o.id), 0),
                   o.customer_name, o.customer_phone, o.customer_address
              FROM orders o
             WHERE o.id = @orderId AND o.user_id = @userId
             LIMIT 1
            """, connection))
        {
            command.Parameters.AddWithValue("@orderId", orderId);
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                result = new VehicleOrderDetails
                {
                    Order = ReadSummary(reader),
                    CustomerName = reader.GetString(6),
                    CustomerPhone = reader.GetString(7),
                    CustomerAddress = reader.GetString(8)
                };
            }
        }
        if (result is null) return null;

        await using (var command = new MySqlCommand("""
            SELECT oi.product_name, oi.unit_price, oi.quantity
              FROM order_items oi
              JOIN orders o ON o.id = oi.order_id
             WHERE o.id = @orderId AND o.user_id = @userId
             ORDER BY oi.id
            """, connection))
        {
            command.Parameters.AddWithValue("@orderId", orderId);
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Items.Add(new VehicleOrderItem
                {
                    ProductName = reader.GetString(0),
                    UnitPrice = reader.GetDecimal(1),
                    Quantity = reader.GetInt32(2)
                });
            }
        }
        return result;
    }
}
