using System.Data;
using HongAnhShop.Models;
using MySqlConnector;

namespace HongAnhShop.Data;

/// <summary>
/// Read orders and change lifecycle. Cancellation restores stock in the same
/// transaction as the status change, so repeated submissions cannot double-restore.
/// </summary>
public sealed class AdminOrderRepository
{
    private readonly IConfiguration _config;

    public AdminOrderRepository(IConfiguration config) => _config = config;

    private MySqlConnection OpenConnection()
    {
        var password = _config["MYSQL_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Chưa cấu hình MYSQL_PASSWORD.");

        var connectionString = new MySqlConnectionStringBuilder
        {
            Server = _config["MYSQL_HOST"] ?? "127.0.0.1",
            Port = 3306,
            Database = "honganhshop",
            UserID = "shop_app",
            Password = password
        }.ConnectionString;

        return new MySqlConnection(connectionString);
    }

    private static AdminOrder ReadOrder(MySqlDataReader r) => new()
    {
        Id = r.GetInt32(0),
        OrderCode = r.GetString(1),
        CustomerName = r.GetString(2),
        CustomerPhone = r.GetString(3),
        CustomerAddress = r.GetString(4),
        TotalAmount = r.GetDecimal(5),
        Status = r.GetString(6),
        CreatedAt = r.GetDateTime(7)
    };

    private const string OrderColumns =
        "id, order_code, customer_name, customer_phone, customer_address, " +
        "total_amount, status, created_at";

    public async Task<AdminOrdersPageVm> GetPageAsync(string? search, string? status)
    {
        search = (search ?? "").Trim();
        if (search.Length > 100) search = search[..100];
        status = (status ?? "").Trim().ToLowerInvariant();
        if (status is not ("pending" or "confirmed" or "completed" or "cancelled"))
            status = "";

        var result = new AdminOrdersPageVm { Search = search, StatusFilter = status };
        await using var connection = OpenConnection();
        await connection.OpenAsync();

        await using (var statsCommand = new MySqlCommand(@"
            SELECT COUNT(*),
                COALESCE(SUM(status = 'pending'), 0),
                COALESCE(SUM(status = 'confirmed'), 0),
                COALESCE(SUM(status = 'completed'), 0),
                COALESCE(SUM(status = 'cancelled'), 0),
                COALESCE(SUM(CASE WHEN status = 'completed' THEN total_amount ELSE 0 END), 0)
            FROM orders", connection))
        await using (var reader = await statsCommand.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                result.Stats = new AdminOrderStats
                {
                    Total = Convert.ToInt32(reader.GetValue(0)),
                    Pending = Convert.ToInt32(reader.GetValue(1)),
                    Confirmed = Convert.ToInt32(reader.GetValue(2)),
                    Completed = Convert.ToInt32(reader.GetValue(3)),
                    Cancelled = Convert.ToInt32(reader.GetValue(4)),
                    CompletedValue = reader.GetDecimal(5)
                };
            }
        }

        await using (var listCommand = new MySqlCommand($@"
            SELECT {OrderColumns}
            FROM orders
            WHERE (@status = '' OR status = @status)
              AND (@search = '' OR order_code LIKE @pattern
                OR customer_name LIKE @pattern OR customer_phone LIKE @pattern)
            ORDER BY created_at DESC, id DESC
            LIMIT 200", connection))
        {
            listCommand.Parameters.AddWithValue("@status", status);
            listCommand.Parameters.AddWithValue("@search", search);
            listCommand.Parameters.AddWithValue("@pattern", $"%{search}%");
            await using var reader = await listCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync()) result.Orders.Add(ReadOrder(reader));
        }

        return result;
    }

    public async Task<AdminOrderDetailsVm?> GetDetailsAsync(int id)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        AdminOrder? order;

        await using (var orderCommand = new MySqlCommand(
            $"SELECT {OrderColumns} FROM orders WHERE id = @id LIMIT 1", connection))
        {
            orderCommand.Parameters.AddWithValue("@id", id);
            await using var reader = await orderCommand.ExecuteReaderAsync();
            order = await reader.ReadAsync() ? ReadOrder(reader) : null;
        }

        if (order is null) return null;
        var result = new AdminOrderDetailsVm { Order = order };

        await using (var linesCommand = new MySqlCommand(@"
            SELECT id, product_id, product_name, unit_price, quantity
            FROM order_items WHERE order_id = @id ORDER BY id", connection))
        {
            linesCommand.Parameters.AddWithValue("@id", id);
            await using var reader = await linesCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Lines.Add(new AdminOrderLine
                {
                    Id = reader.GetInt32(0),
                    ProductId = reader.GetInt32(1),
                    ProductName = reader.GetString(2),
                    UnitPrice = reader.GetDecimal(3),
                    Quantity = reader.GetInt32(4)
                });
            }
        }

        return result;
    }

    public async Task<AdminOrderStatusResult> ChangeStatusAsync(int orderId, string nextStatus)
    {
        // The controller also validates the input, but enforce this at the data layer.
        if (nextStatus is not ("confirmed" or "completed" or "cancelled"))
            return AdminOrderStatusResult.InvalidTransition;

        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var transaction = (MySqlTransaction)await connection
            .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            string? currentStatus;
            await using (var statusCommand = new MySqlCommand(
                "SELECT status FROM orders WHERE id = @id FOR UPDATE", connection, transaction))
            {
                statusCommand.Parameters.AddWithValue("@id", orderId);
                currentStatus = (string?)await statusCommand.ExecuteScalarAsync();
            }

            if (currentStatus is null)
                return AdminOrderStatusResult.NotFound;
            if (currentStatus == nextStatus)
                return AdminOrderStatusResult.AlreadySet;

            var allowed = (currentStatus == "pending" &&
                           (nextStatus == "confirmed" || nextStatus == "cancelled")) ||
                          (currentStatus == "confirmed" &&
                           (nextStatus == "completed" || nextStatus == "cancelled"));
            if (!allowed)
                return AdminOrderStatusResult.InvalidTransition;

            if (nextStatus == "cancelled")
            {
                // Load all lines first, then release the reader before issuing UPDATEs.
                var lines = new List<(int ProductId, int Quantity)>();
                await using (var linesCommand = new MySqlCommand(@"
                    SELECT product_id, SUM(quantity) AS qty
                    FROM order_items
                    WHERE order_id = @id
                    GROUP BY product_id
                    ORDER BY product_id", connection, transaction))
                {
                    linesCommand.Parameters.AddWithValue("@id", orderId);
                    await using var reader = await linesCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        lines.Add((reader.GetInt32(0), Convert.ToInt32(reader.GetValue(1))));
                    }
                }

                if (lines.Count == 0)
                    throw new InvalidOperationException("Đơn hàng không có chi tiết; chưa thể hủy an toàn.");

                foreach (var (productId, quantity) in lines)
                {
                    await using var stockCommand = new MySqlCommand(@"
                        UPDATE products SET stock = stock + @qty
                        WHERE id = @productId", connection, transaction);
                    stockCommand.Parameters.AddWithValue("@qty", quantity);
                    stockCommand.Parameters.AddWithValue("@productId", productId);

                    if (await stockCommand.ExecuteNonQueryAsync() != 1)
                        throw new InvalidOperationException(
                            $"Không tìm thấy xe #{productId} để hoàn tồn kho. Đã hủy thao tác.");
                }
            }

            await using (var updateCommand = new MySqlCommand(@"
                UPDATE orders SET status = @next
                WHERE id = @id AND status = @current", connection, transaction))
            {
                updateCommand.Parameters.AddWithValue("@next", nextStatus);
                updateCommand.Parameters.AddWithValue("@id", orderId);
                updateCommand.Parameters.AddWithValue("@current", currentStatus);
                if (await updateCommand.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException("Đơn hàng đã thay đổi, vui lòng tải lại.");
            }

            await transaction.CommitAsync();
            return AdminOrderStatusResult.Updated;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        // On early returns, disposing the active transaction rolls back automatically.
    }
}
