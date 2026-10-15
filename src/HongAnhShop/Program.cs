using Microsoft.AspNetCore.Authentication;

using HongAnhShop.Data;
using HongAnhShop.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// =======================================
// 1. ASP.NET Core MVC
// =======================================
builder.Services.AddControllersWithViews();

// =======================================
// 2. Repository - MySQL
// =======================================
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<AdminOrderRepository>();
// Repository quản lý tài khoản
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<AdminCustomerRepository>();
builder.Services.AddScoped<AdminStaffRepository>();
builder.Services.AddScoped<VehicleOrderHistoryRepository>();

// =======================================
// 3. Mã hóa mật khẩu người dùng
// =======================================
builder.Services.AddScoped<
    IPasswordHasher<ShopUser>,
    PasswordHasher<ShopUser>>();

// =======================================
// 4. Xác thực tài khoản bằng Cookie
// =======================================
builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AURELIA.Auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.SameSite = SameSiteMode.Lax;

        // Chạy được với HTTP trong môi trường
        // Ubuntu local hiện tại.
        // Khi triển khai HTTPS chính thức,
        // nên chuyển sang Always.
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;

        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath =
            "/Account/Denied";

        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        options.SlidingExpiration = true;

        // Kiểm tra role/is_active từ MySQL trên từng yêu cầu đã đăng nhập.
        // Nếu Admin khóa User hoặc thay quyền: cookie cũ không còn dùng được.
        options.Events.OnValidatePrincipal = async context =>
        {
            var idText = context.Principal?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idText, out var id) || id <= 0)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }
            var repository = context.HttpContext.RequestServices
                .GetRequiredService<AdminCustomerRepository>();
            var state = await repository.GetSecurityStateAsync(id);
            var claimedRole = context.Principal?.FindFirst(
                System.Security.Claims.ClaimTypes.Role)?.Value;
            if (state == null || !state.IsActive ||
                !string.Equals(state.Role, claimedRole, StringComparison.Ordinal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// =======================================
// 5. Phân quyền Admin / User
// =======================================
builder.Services.AddAuthorization();

// =======================================
// 6. Session lưu giỏ hàng của khách
// =======================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;

    options.Cookie.SameSite =
        SameSiteMode.Lax;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.SameAsRequest;
});

// =======================================
// 7. Khởi tạo ứng dụng
// =======================================
var app = builder.Build();

// =======================================
// 8. Xử lý lỗi
// =======================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// =======================================
// 9. Middleware
// =======================================
app.UseRouting();

// Session giỏ hàng
app.UseSession();

// Xác thực tài khoản
app.UseAuthentication();

// Kiểm tra quyền truy cập
app.UseAuthorization();

// =======================================
// 10. Static Assets
// =======================================
app.MapStaticAssets();

// =======================================
// 11. MVC Routing
// =======================================
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// =======================================
// 12. Chạy ứng dụng
// =======================================
app.Run();
