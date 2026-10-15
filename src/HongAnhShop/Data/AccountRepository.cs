using HongAnhShop.Models;
using MySqlConnector;

namespace HongAnhShop.Data;

/// <summary>
/// Uses the same MySQL database as ProductRepository; credentials are read from config/environment.
/// No database schema is created or deleted automatically.
/// </summary>
public sealed class AccountRepository
{
    private readonly IConfiguration _config;

    public AccountRepository(IConfiguration config) => _config = config;

    private MySqlConnection OpenConnection()
    {
        var password = _config["MYSQL_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Chưa có MYSQL_PASSWORD trong môi trường ứng dụng.");

        var host = _config["MYSQL_HOST"] ?? "127.0.0.1";
        var port = uint.TryParse(_config["MYSQL_PORT"], out var p) ? p : 3306;
        var cs = new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = port,
            Database = _config["MYSQL_DATABASE"] ?? "honganhshop",
            UserID = _config["MYSQL_USER"] ?? "shop_app",
            Password = password,
            CharacterSet = "utf8mb4"
        }.ConnectionString;
        return new MySqlConnection(cs);
    }

    public async Task<ShopUser?> FindAsync(string identifier)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            SELECT id, full_name, email, phone, password_hash, role,
                   is_active, failed_login_attempts, locked_until_utc
              FROM users
             WHERE email = @identity OR phone = @identity
             LIMIT 1
            """, connection);
        cmd.Parameters.AddWithValue("@identity", identifier);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new ShopUser
        {
            Id = reader.GetInt32(0),
            FullName = reader.GetString(1),
            Email = reader.IsDBNull(2) ? null : reader.GetString(2),
            Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
            PasswordHash = reader.GetString(4),
            Role = reader.GetString(5),
            IsActive = reader.GetBoolean(6),
            FailedLoginAttempts = reader.GetInt32(7),
            LockedUntilUtc = reader.IsDBNull(8) ? null : DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc)
        };
    }

    public async Task CreateAsync(ShopUser user)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            INSERT INTO users(full_name, email, phone, password_hash, role, is_active)
            VALUES (@name, @email, @phone, @hash, 'User', 1)
            """, connection);
        cmd.Parameters.AddWithValue("@name", user.FullName);
        cmd.Parameters.AddWithValue("@email", (object?)user.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)user.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@hash", user.PasswordHash);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkFailedLoginAsync(int userId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            UPDATE users
               SET locked_until_utc = CASE
                   WHEN failed_login_attempts + 1 >= 5 THEN UTC_TIMESTAMP(6) + INTERVAL 15 MINUTE
                   ELSE NULL END,
                   failed_login_attempts = failed_login_attempts + 1
             WHERE id = @id
            """, connection);
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetFailedLoginAsync(int userId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            UPDATE users SET failed_login_attempts = 0, locked_until_utc = NULL
             WHERE id = @id
            """, connection);
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync();
    }
}
