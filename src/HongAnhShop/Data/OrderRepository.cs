using System.Data;
using HongAnhShop.Models;
using MySqlConnector;

namespace HongAnhShop.Data;

public class OrderRepository
{
    private readonly IConfiguration _config;

    public OrderRepository(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> CreateAsync(
        CheckoutInput customer,
        List<CartItem> cart,
        int? userId = null)
    {
        if (cart.Count == 0)
            throw new InvalidOperationException(
                "Gio hang dang trong.");

        if (cart.Any(x => x.ProductId <= 0 ||
                          x.Quantity <= 0 ||
                          x.Quantity > 100) ||
            cart.Select(x => x.ProductId).Distinct().Count()
                != cart.Count)
            throw new InvalidOperationException(
                "Du lieu gio hang khong hop le.");

        var password = _config["MYSQL_PASSWORD"];

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "Chua cau hinh MYSQL_PASSWORD.");

        var connectionString = new MySqlConnectionStringBuilder
        {
            Server = _config["MYSQL_HOST"] ?? "127.0.0.1",
            Port = 3306,
            Database = "honganhshop",
            UserID = "shop_app",
            Password = password
        }.ConnectionString;

        await using var connection =
            new MySqlConnection(connectionString);

        await connection.OpenAsync();

        await using var transaction =
            (MySqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        // Khoá dòng tài khoản cho tới khi COMMIT, chống gắn đơn khi bị khoá/đổi quyền.
        if (userId is int uid)
        {
            await using var userCommand = new MySqlCommand(
                "SELECT id FROM users WHERE id = @uid AND role = 'User' AND is_active = 1 FOR SHARE",
                connection, transaction);
            userCommand.Parameters.AddWithValue("@uid", uid);
            if (await userCommand.ExecuteScalarAsync() is null)
                throw new InvalidOperationException("Tài khoản không còn hợp lệ. Hãy đăng nhập lại.");
        }

        var lines = new List<(
            int Id, string Name, decimal Price, int Quantity)>();

        // Doc gia va ton kho that, khoa hang khi dat
        foreach (var item in cart.OrderBy(x => x.ProductId))
        {
            using var command = new MySqlCommand(
                @"SELECT name, price, stock
                  FROM products
                  WHERE id = @id
                    AND is_active = 1
                  FOR UPDATE",
                connection, transaction);

            command.Parameters.AddWithValue("@id", item.ProductId);

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new InvalidOperationException(
                    "San pham khong con ton tai.");

            var name = reader.GetString(0);
            var price = reader.GetDecimal(1);
            var stock = reader.GetInt32(2);

            if (item.Quantity > stock)
                throw new InvalidOperationException(
                    $"San pham {name} khong du ton kho.");

            lines.Add((item.ProductId, name, price, item.Quantity));
        }

        var total = lines.Sum(x => x.Price * x.Quantity);
        var orderCode = Guid.NewGuid().ToString("N");

        // Tao don hang
        using var orderCommand = new MySqlCommand(
            @"INSERT INTO orders
              (order_code, user_id, customer_name, customer_phone,
               customer_address, total_amount, status)
              VALUES
              (@code, @userId, @name, @phone, @address, @total, 'pending')",
            connection, transaction);

        orderCommand.Parameters.AddWithValue("@code", orderCode);
        orderCommand.Parameters.AddWithValue("@userId", (object?)userId ?? DBNull.Value);
        orderCommand.Parameters.AddWithValue(
            "@name", customer.CustomerName.Trim());
        orderCommand.Parameters.AddWithValue(
            "@phone", customer.CustomerPhone.Trim());
        orderCommand.Parameters.AddWithValue(
            "@address", customer.CustomerAddress.Trim());
        orderCommand.Parameters.AddWithValue("@total", total);

        await orderCommand.ExecuteNonQueryAsync();

        var orderId = orderCommand.LastInsertedId;

        foreach (var line in lines)
        {
            // Tru ton kho
            using var stockCommand = new MySqlCommand(
                @"UPDATE products
                  SET stock = stock - @qty
                  WHERE id = @id AND stock >= @qty",
                connection, transaction);

            stockCommand.Parameters.AddWithValue("@qty", line.Quantity);
            stockCommand.Parameters.AddWithValue("@id", line.Id);

            if (await stockCommand.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException(
                    "Khong du ton kho de dat hang.");

            // Luu chi tiet don hang
            using var itemCommand = new MySqlCommand(
                @"INSERT INTO order_items
                  (order_id, product_id, product_name,
                   unit_price, quantity)
                  VALUES
                  (@orderId, @productId, @name, @price, @qty)",
                connection, transaction);

            itemCommand.Parameters.AddWithValue("@orderId", orderId);
            itemCommand.Parameters.AddWithValue("@productId", line.Id);
            itemCommand.Parameters.AddWithValue("@name", line.Name);
            itemCommand.Parameters.AddWithValue("@price", line.Price);
            itemCommand.Parameters.AddWithValue("@qty", line.Quantity);

            await itemCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();

        return orderCode;
    }
}