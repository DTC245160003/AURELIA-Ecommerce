#!/usr/bin/env python3
# Bổ sung DI + cookie validation + liên kết menu. Không ghi đè file khác.
from pathlib import Path
from datetime import datetime
import sys
root = Path(__file__).resolve().parent
program = root / "src/HongAnhShop/Program.cs"
dashboard = root / "src/HongAnhShop/Views/Admin/Index.cshtml"
for p in (program, dashboard):
    if not p.is_file():
        sys.exit(f"THIEU FILE: {p} - vui long chay script trong thu muc du an")
code = program.read_text(encoding="utf-8")
page = dashboard.read_text(encoding="utf-8")
if "AddScoped<AdminCustomerRepository>" not in code:
    needle = "builder.Services.AddScoped<AccountRepository>();"
    if code.count(needle)!=1: sys.exit("Khong tim thay duy nhat dong AddScoped<AccountRepository> - chua sua file nao")
    code=code.replace(needle, needle+"\nbuilder.Services.AddScoped<AdminCustomerRepository>();")
if "options.Events.OnValidatePrincipal" not in code:
    anchor="options.SlidingExpiration = true;"
    if code.count(anchor)!=1: sys.exit("Khong tim thay SlidingExpiration trong Program.cs - chua sua file nao")
    block=r'''

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
        };'''
    code=code.replace(anchor,anchor+block)
    if "using Microsoft.AspNetCore.Authentication;" not in code:
        code="using Microsoft.AspNetCore.Authentication;\n"+code
if '<a class="nav-link" href="/Admin/Customers">' not in page:
    old='<span class="nav-link disabled">\n                    ◇ Quản lý khách hàng\n                </span>'
    new='<a class="nav-link" href="/Admin/Customers">\n                    ◇ Quản lý khách hàng\n                </a>'
    if page.count(old)!=1:sys.exit("Khong tim thay menu Khach hang mac dinh - chua sua file nao")
    page=page.replace(old,new)
# This only changes dashboard text for matching known version
page=page.replace('<span class="card-status">SẮP TRIỂN KHAI</span>', '<div class="card-status">ĐANG HOẠT ĐỘNG</div>\n                    <div><a class="card-action" href="/Admin/Customers">Mở quản lý khách hàng →</a></div>', 1)
page=page.replace('Quản lý siêu xe và đơn hàng đã được kết nối.\n                Khách hàng và phân quyền nhân viên sẽ được triển khai tiếp.', 'Quản lý siêu xe, đơn hàng và khách hàng đã kết nối MySQL.\n                Phân quyền nhân viên sẽ được triển khai tiếp.')
page=page.replace('Quản lý siêu xe và quản lý đơn hàng\n                đã kết nối MySQL. Quản lý khách hàng và phân quyền\n                nhân viên đang chờ triển khai.', 'Quản lý siêu xe, đơn hàng và khách hàng đã kết nối\n                MySQL. Phân quyền nhân viên đang chờ triển khai.')
backup = root / "aurelia-backups" / ("customers-" + datetime.now().strftime("%Y%m%d-%H%M%S"))
backup.mkdir(parents=True, exist_ok=True)
(backup/"Program.cs").write_text(program.read_text(encoding="utf-8"),encoding="utf-8")
(backup/"Admin.Index.cshtml").write_text(dashboard.read_text(encoding="utf-8"),encoding="utf-8")
program.write_text(code,encoding="utf-8")
dashboard.write_text(page,encoding="utf-8")
print("OK: Program.cs va Admin Dashboard da cap nhat")
print("Ban sao luu:", backup)
