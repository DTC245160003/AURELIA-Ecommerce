using HongAnhShop.Models;
using MySqlConnector;

namespace HongAnhShop.Data;

/// <summary>
/// Admin-only operations. SQL limits changes to User/Staff; never touches Admin.
/// </summary>
public sealed class AdminStaffRepository
{
    private readonly IConfiguration _config;
    public AdminStaffRepository(IConfiguration config) => _config = config;

    private MySqlConnection OpenConnection()
    {
        var password = _config["MYSQL_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Chưa có MYSQL_PASSWORD.");

        var connectionString = new MySqlConnectionStringBuilder
        {
            Server = _config["MYSQL_HOST"] ?? "127.0.0.1",
            Port = uint.TryParse(_config["MYSQL_PORT"], out var port) ? port : 3306,
            Database = _config["MYSQL_DATABASE"] ?? "honganhshop",
            UserID = _config["MYSQL_USER"] ?? "shop_app",
            Password = password,
            CharacterSet = "utf8mb4"
        }.ConnectionString;
        return new MySqlConnection(connectionString);
    }

    private const string SelectColumns = """
        SELECT id, full_name, email, phone, role, is_active, created_at_utc
          FROM users WHERE role IN ('User', 'Staff')
        """;

    private static AdminStaffUserRow ReadRow(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        FullName = reader.GetString(1),
        Email = reader.IsDBNull(2) ? null : reader.GetString(2),
        Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
        Role = reader.GetString(4),
        IsActive = reader.GetBoolean(5),
        CreatedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)
    };

    public async Task<AdminStaffPage> GetPageAsync(string? query, string? filter)
    {
        var search = (query ?? "").Trim();
        if (search.Length > 80) search = search[..80];
        var selected = filter is "User" or "Staff" ? filter : "all";
        var page = new AdminStaffPage { Query = search, Filter = selected };

        await using var connection = OpenConnection();
        await connection.OpenAsync();

        await using (var count = new MySqlCommand("""
            SELECT COALESCE(SUM(role = 'User'), 0),
                   COALESCE(SUM(role = 'Staff'), 0)
              FROM users WHERE role IN ('User','Staff')
            """, connection))
        await using (var reader = await count.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                page.TotalUsers = Convert.ToInt32(reader.GetValue(0));
                page.TotalStaff = Convert.ToInt32(reader.GetValue(1));
            }
        }

        var sql = SelectColumns;
        if (search.Length > 0)
            sql += " AND (full_name LIKE @q ESCAPE '!' OR email LIKE @q ESCAPE '!' OR phone LIKE @q ESCAPE '!')";
        if (selected != "all") sql += " AND role = @role";
        sql += " ORDER BY CASE WHEN role = 'Staff' THEN 0 ELSE 1 END, id DESC LIMIT 100";
        await using var command = new MySqlCommand(sql, connection);
        if (search.Length > 0)
        {
            var escaped = search.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_");
            command.Parameters.AddWithValue("@q", "%" + escaped + "%");
        }
        if (selected != "all") command.Parameters.AddWithValue("@role", selected);
        await using var users = await command.ExecuteReaderAsync();
        while (await users.ReadAsync()) page.Accounts.Add(ReadRow(users));
        return page;
    }

    public async Task<AdminStaffUserRow?> GetAsync(int id)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand(SelectColumns + " AND id = @id LIMIT 1", connection);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadRow(reader) : null;
    }

    public async Task<AdminStaffChangeResult> ChangeRoleAsync(int id, string expectedRole, string nextRole)
    {
        if (id <= 0 ||
            !((expectedRole == "User" && nextRole == "Staff") ||
              (expectedRole == "Staff" && nextRole == "User")))
            return AdminStaffChangeResult.InvalidTransition;

        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand("""
            UPDATE users
               SET role = @next
             WHERE id = @id
               AND role = @expected
               AND role IN ('User', 'Staff')
               AND (@next = 'User' OR is_active = 1)
            """, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@next", nextRole);
        command.Parameters.AddWithValue("@expected", expectedRole);
        if (await command.ExecuteNonQueryAsync() == 1)
            return AdminStaffChangeResult.Updated;

        var current = await GetAsync(id);
        if (current is null) return AdminStaffChangeResult.NotFound;
        if (nextRole == "Staff" && !current.IsActive)
            return AdminStaffChangeResult.InactiveAccount;
        return AdminStaffChangeResult.AlreadyChanged;
    }
}
