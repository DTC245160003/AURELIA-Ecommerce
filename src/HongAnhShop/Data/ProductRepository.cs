using HongAnhShop.Models;
using MySqlConnector;

namespace HongAnhShop.Data;

public class ProductRepository
{
    private readonly IConfiguration _config;

    public ProductRepository(IConfiguration config) => _config = config;

    private MySqlConnection OpenConnection()
    {
        var password = _config["MYSQL_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Chua cau hinh MYSQL_PASSWORD");

        var cs = new MySqlConnectionStringBuilder
        {
            Server = _config["MYSQL_HOST"] ?? "127.0.0.1",
            Port = 3306,
            Database = "honganhshop",
            UserID = "shop_app",
            Password = password
        }.ConnectionString;
        return new MySqlConnection(cs);
    }

    private const string SelectColumns = @"
        SELECT id, name, category, description, price,
               image_url, stock, is_active
        FROM products";

    private static Product ReadProduct(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Category = reader.GetString(2),
        Description = reader.GetString(3),
        Price = reader.GetDecimal(4),
        ImageUrl = reader.GetString(5),
        Stock = reader.GetInt32(6),
        IsActive = reader.GetBoolean(7)
    };

    // Dung cho trang mua hang: chi hien thi xe dang ban.
    public Task<List<Product>> GetAllAsync() => GetListAsync(onlyActive: true);

    // Dung cho Admin/Staff: xem ca xe dang an.
    public Task<List<Product>> GetAllForAdminAsync() => GetListAsync(onlyActive: false);

    private async Task<List<Product>> GetListAsync(bool onlyActive)
    {
        var results = new List<Product>();
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = SelectColumns
            + (onlyActive ? " WHERE is_active = 1" : "")
            + " ORDER BY id DESC";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) results.Add(ReadProduct(reader));
        return results;
    }

    // Dung cho gio hang/trang chi tiet: khong cho lay xe ngung ban.
    public Task<Product?> GetByIdAsync(int id) => FindAsync(id, onlyActive: true);

    public Task<Product?> GetByIdForAdminAsync(int id) => FindAsync(id, onlyActive: false);

    private async Task<Product?> FindAsync(int id, bool onlyActive)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = SelectColumns
            + " WHERE id = @id"
            + (onlyActive ? " AND is_active = 1" : "")
            + " LIMIT 1";
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadProduct(reader) : null;
    }

    public async Task<int> CreateAsync(ProductInput input)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO products
                (name, category, description, price, image_url, stock, is_active)
            VALUES
                (@name, @category, @description, @price, @image, @stock, 1)";
        BindProduct(command, input);
        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    public async Task<bool> UpdateAsync(int id, ProductInput input)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE products
            SET name = @name, category = @category,
                description = @description, price = @price,
                image_url = @image, stock = @stock
            WHERE id = @id";
        BindProduct(command, input);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
        return true; // Controller da kiem tra id ton tai truoc khi cap nhat.
    }

    public async Task<bool> SetActiveAsync(int id, bool isActive)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE products SET is_active = @active WHERE id = @id";
        command.Parameters.AddWithValue("@active", isActive);
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static void BindProduct(MySqlCommand command, ProductInput input)
    {
        command.Parameters.AddWithValue("@name", input.Name.Trim());
        command.Parameters.AddWithValue("@category", input.Category.Trim());
        command.Parameters.AddWithValue("@description", input.Description.Trim());
        command.Parameters.AddWithValue("@price", input.Price);
        command.Parameters.AddWithValue("@image", input.ImageUrl.Trim());
        command.Parameters.AddWithValue("@stock", input.Stock);
    }
}
