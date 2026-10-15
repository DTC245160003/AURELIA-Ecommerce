using HongAnhShop.Models;
using MySqlConnector;

namespace HongAnhShop.Data;

public sealed class AdminCustomerRepository
{
    private readonly IConfiguration _config;
    public AdminCustomerRepository(IConfiguration config) => _config = config;

    private MySqlConnection OpenConnection()
    {
        var password = _config["MYSQL_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Chưa cấu hình MYSQL_PASSWORD.");

        var cs = new MySqlConnectionStringBuilder
        {
            Server = _config["MYSQL_HOST"] ?? "127.0.0.1",
            Port = uint.TryParse(_config["MYSQL_PORT"], out var port) ? port : 3306,
            Database = _config["MYSQL_DATABASE"] ?? "honganhshop",
            UserID = _config["MYSQL_USER"] ?? "shop_app",
            Password = password,
            CharacterSet = "utf8mb4"
        }.ConnectionString;
        return new MySqlConnection(cs);
    }

    // Được gọi từ cookie authentication: kiểm tra cả trạng thái và quyền từ DB.
    public async Task<CustomerSecurityState?> GetSecurityStateAsync(int userId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(
            "SELECT role, is_active FROM users WHERE id = @id LIMIT 1", connection);
        command.Parameters.AddWithValue("@id", userId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new CustomerSecurityState
        {
            Role = reader.GetString(0),
            IsActive = reader.GetBoolean(1)
        };
    }

    private const string CustomerSelect = """
        SELECT id, full_name, email, phone, is_active,
               failed_login_attempts, locked_until_utc
          FROM users
         WHERE role = 'User'
        """;

    private static AdminCustomerRow ReadCustomer(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        FullName = reader.GetString(1),
        Email = reader.IsDBNull(2) ? null : reader.GetString(2),
        Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
        IsActive = reader.GetBoolean(4),
        FailedLoginAttempts = reader.GetInt32(5),
        LockedUntilUtc = reader.IsDBNull(6) ? null :
            DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)
    };

    public async Task<AdminCustomersPage> GetPageAsync(string? q, string? status)
    {
        var search = (q ?? "").Trim();
        if (search.Length > 80) search = search[..80];
        var filter = status is "active" or "inactive" ? status : "all";
        var page = new AdminCustomersPage { Query = search, Status = filter };

        await using var connection = OpenConnection();
        await connection.OpenAsync();

        await using (var count = new MySqlCommand("""
            SELECT COUNT(*), COALESCE(SUM(is_active = 1), 0),
                   COALESCE(SUM(is_active = 0), 0)
              FROM users WHERE role = 'User'
            """, connection))
        await using (var reader = await count.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                page.Total = Convert.ToInt32(reader.GetValue(0));
                page.Active = Convert.ToInt32(reader.GetValue(1));
                page.Inactive = Convert.ToInt32(reader.GetValue(2));
            }
        }

        var sql = CustomerSelect;
        if (search.Length > 0)
            sql += " AND (full_name LIKE @q ESCAPE '!' OR email LIKE @q ESCAPE '!' OR phone LIKE @q ESCAPE '!')";
        if (filter == "active") sql += " AND is_active = 1";
        if (filter == "inactive") sql += " AND is_active = 0";
        sql += " ORDER BY id DESC LIMIT 100";

        await using var command = new MySqlCommand(sql, connection);
        if (search.Length > 0)
        {
            var escaped = search.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_");
            command.Parameters.AddWithValue("@q", "%" + escaped + "%");
        }

        await using var users = await command.ExecuteReaderAsync();
        while (await users.ReadAsync()) page.Customers.Add(ReadCustomer(users));
        return page;
    }

    public async Task<AdminCustomerRow?> GetAsync(int id)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand(CustomerSelect + " AND id = @id LIMIT 1", connection);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadCustomer(reader) : null;
    }

    // SQL tự bảo vệ: chỉ sửa role User, không đụng tới Admin hoặc Staff.
    public async Task<bool> SetActiveAsync(int userId, bool isActive)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            UPDATE users
               SET is_active = @active,
                   failed_login_attempts = IF(@active = 1, 0, failed_login_attempts),
                   locked_until_utc = IF(@active = 1, NULL, locked_until_utc)
             WHERE id = @id AND role = 'User' AND is_active <> @active
            """, connection);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
        return await cmd.ExecuteNonQueryAsync() == 1;
    }
}
