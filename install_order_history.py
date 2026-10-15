#!/usr/bin/env python3
"""Install AURELIA account-linked order history (run at project root).

Does not run migrations. Run MIGRATE_ORDER_USER.sql first as explained in README.
Writes original source files to a timestamped backup before changing them.
"""
from datetime import datetime
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parent
prefix = root / 'src/HongAnhShop'
program = prefix / 'Program.cs'
cart = prefix / 'Controllers/CartController.cs'
orders = prefix / 'Data/OrderRepository.cs'
customer_controller = prefix / 'Controllers/AdminCustomersController.cs'
customer_view = prefix / 'Views/Admin/CustomerDetails.cshtml'
cart_index = prefix / 'Views/Cart/Index.cshtml'
success_view = prefix / 'Views/Cart/Success.cshtml'
required = [program, cart, orders, customer_controller, customer_view]
for path in required:
    if not path.is_file():
        sys.exit(f'THIEU FILE: {path}. Chua thay doi file nao.')
originals = {p: p.read_text(encoding='utf-8') for p in required}
# We support the existing cart/success views but don't require their precise HTML layout.
for optional in (cart_index, success_view):
    if optional.is_file():
        originals[optional] = optional.read_text(encoding='utf-8')
contents = dict(originals)

def replace_exact(path, old, new, already=None):
    txt = contents[path]
    if already and already in txt:
        return
    if txt.count(old) != 1:
        sys.exit(f'Khong tim thay doan code duy nhat trong {path.relative_to(root)}. Khong thay doi file nao.\nCAN TIM: {old[:180]!r}')
    contents[path] = txt.replace(old, new, 1)

# DI: only add new repo, preserving Admin/Staff role checks and cookie validation.
p = contents[program]
service = 'builder.Services.AddScoped<VehicleOrderHistoryRepository>();'
if service not in p:
    anchor = next((x for x in (
        'builder.Services.AddScoped<AdminStaffRepository>();',
        'builder.Services.AddScoped<AdminCustomerRepository>();',
        'builder.Services.AddScoped<OrderRepository>();') if x in p), None)
    if not anchor:
        sys.exit('Khong tim thay cho dang ky repository trong Program.cs. Chua sua file nao.')
    replace_exact(program, anchor, anchor + '\n' + service)

# Cart POST: use only claim from the authenticated cookie; never accept user ID from HTML/form.
p = contents[cart]
if 'using System.Security.Claims;' not in p:
    replace_exact(cart, 'using System.Text.Json;', 'using System.Security.Claims;\nusing System.Text.Json;')
old_checkout = '''                var orderCode =
                    await _orders.CreateAsync(input, cart);'''
new_checkout = '''                // Chỉ gắn đơn với User hiện đang xác thực; không nhận userId từ form.
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true && User.IsInRole("User"))
                {
                    if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)
                        || parsedId <= 0)
                        throw new InvalidOperationException("Phiên đăng nhập không hợp lệ. Hãy đăng nhập lại.");
                    userId = parsedId;
                }

                var orderCode =
                    await _orders.CreateAsync(input, cart, userId);'''
replace_exact(cart, old_checkout, new_checkout,
              already='await _orders.CreateAsync(input, cart, userId);')

# OrderRepository: preserve stock locks, unit price reads, transaction and rollback semantics.
replace_exact(orders, '''        List<CartItem> cart)
    {''', '''        List<CartItem> cart,
        int? userId = null)
    {''', already='int? userId = null)')

needle = '''        var lines = new List<(
            int Id, string Name, decimal Price, int Quantity)>();'''
validation = '''        // Khoá dòng tài khoản cho tới khi COMMIT, chống gắn đơn khi bị khoá/đổi quyền.
        if (userId is int uid)
        {
            await using var userCommand = new MySqlCommand(
                "SELECT id FROM users WHERE id = @uid AND role = 'User' AND is_active = 1 FOR SHARE",
                connection, transaction);
            userCommand.Parameters.AddWithValue("@uid", uid);
            if (await userCommand.ExecuteScalarAsync() is null)
                throw new InvalidOperationException("Tài khoản không còn hợp lệ. Hãy đăng nhập lại.");
        }

'''
replace_exact(orders, needle, validation + needle,
              already='// Khoá dòng tài khoản cho tới khi COMMIT')
replace_exact(orders, '''(order_code, customer_name, customer_phone,
               customer_address, total_amount, status)
              VALUES
              (@code, @name, @phone, @address, @total, 'pending')''',
              '''(order_code, user_id, customer_name, customer_phone,
               customer_address, total_amount, status)
              VALUES
              (@code, @userId, @name, @phone, @address, @total, 'pending')''',
              already='(@code, @userId, @name, @phone, @address, @total')
replace_exact(orders, '''        orderCommand.Parameters.AddWithValue("@code", orderCode);''',
              '''        orderCommand.Parameters.AddWithValue("@code", orderCode);
        orderCommand.Parameters.AddWithValue("@userId", (object?)userId ?? DBNull.Value);''',
              already='orderCommand.Parameters.AddWithValue("@userId"')

# Customer profile (Admin/Staff only): linked orders precisely by users.id.
p = contents[customer_controller]
if 'VehicleOrderHistoryRepository' not in p:
    replace_exact(customer_controller, '''    private readonly AdminCustomerRepository _customers;
    public AdminCustomersController(AdminCustomerRepository customers) => _customers = customers;''',
'''    private readonly AdminCustomerRepository _customers;
    private readonly VehicleOrderHistoryRepository _history;
    public AdminCustomersController(AdminCustomerRepository customers, VehicleOrderHistoryRepository history)
    {
        _customers = customers;
        _history = history;
    }''')
if 'ViewBag.LinkedOrders' not in contents[customer_controller]:
    replace_exact(customer_controller, '''        return customer is null ? NotFound() :
            View("~/Views/Admin/CustomerDetails.cshtml", customer);''',
'''        if (customer is null) return NotFound();
        ViewBag.LinkedOrders = await _history.GetByUserIdAsync(id, limit: 30);
        return View("~/Views/Admin/CustomerDetails.cshtml", customer);''')

p = contents[customer_view]
if 'ViewBag.LinkedOrders' not in p:
    placeholder = re.compile(r'<p class="note">Chưa hiển thị lịch sử mua xe trên hồ sơ này[\s\S]*?</p>', re.U)
    replacement = '''@{
    var linkedOrders = ViewBag.LinkedOrders as List<HongAnhShop.Models.VehicleOrderSummary>
        ?? new List<HongAnhShop.Models.VehicleOrderSummary>();
}
@if (linkedOrders.Count == 0)
{
    <p class="note">Chưa có đơn được liên kết với tài khoản này. Đơn đặt khi chưa đăng nhập hoặc trước khi nâng cấp sẽ không tự động gắn vào hồ sơ.</p>
}
else
{
    <div class="table-scroll"><table><thead><tr><th>Mã đơn</th><th>Ngày đặt</th><th>Giá trị</th><th>Trạng thái</th><th></th></tr></thead><tbody>
    @foreach (var order in linkedOrders)
    {
        <tr><td class="id">@order.OrderCode</td><td>@order.CreatedAt.ToString("dd/MM/yyyy HH:mm")</td><td>@order.TotalAmount.ToString("N0") ₫</td><td>@order.Status</td><td><a class="action" href="/Admin/Orders/@order.Id">Xem đơn →</a></td></tr>
    }
    </tbody></table></div>
    <p class="note">Tối đa 30 đơn gần nhất được gắn chính xác theo ID tài khoản.</p>
}'''
    p, count = placeholder.subn(lambda _: replacement, p)
    if count != 1:
        sys.exit('Khong tim duoc ghi chu lich su trong CustomerDetails.cshtml. Khong thay doi file nao.')
    contents[customer_view] = p

# Optional UI links in cart and success, without depending on their specific CSS.
optional_notes = []
if cart_index in contents:
    p = contents[cart_index]
    old = '<a href="/Account/Login">Tài khoản</a>'
    if 'href="/Account/MyOrders"' not in p and old in p:
        p = p.replace(old, '@if (User.IsInRole("User")) { <a href="/Account/MyOrders">Đơn hàng của tôi</a> } else { ' + old + ' }', 1)
        contents[cart_index] = p
    elif 'href="/Account/MyOrders"' not in p:
        optional_notes.append('Chua gan link MyOrders vao Cart/Index.cshtml (HTML khac mau).')
if success_view in contents:
    p = contents[success_view]
    marker = '<a href="/Cart" class="btn btn-outline">XEM GIỎ HÀNG</a>'
    if 'href="/Account/MyOrders"' not in p and marker in p:
        p = p.replace(marker, marker + '@if (User.IsInRole("User")) { <a href="/Account/MyOrders" class="btn btn-outline">ĐƠN HÀNG CỦA TÔI</a> }', 1)
        contents[success_view] = p
    elif 'href="/Account/MyOrders"' not in p:
        optional_notes.append('Chua gan link MyOrders vao Cart/Success.cshtml (HTML khac mau).')

changes = [path for path in contents if contents[path] != originals[path]]
if not changes:
    print('OK: Cac thay doi da co trong source. Khong can chay lai.')
    sys.exit(0)

backup = root / 'aurelia-backups' / ('order-history-' + datetime.now().strftime('%Y%m%d-%H%M%S-%f'))
backup.mkdir(parents=True, exist_ok=False)
for path in changes:
    target = backup / path.relative_to(root)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(originals[path], encoding='utf-8')
try:
    for path in changes:
        path.write_text(contents[path], encoding='utf-8')
except Exception:
    for path in changes:
        path.write_text(originals[path], encoding='utf-8')
    raise

print('OK: Da cap nhat:', ', '.join(str(path.relative_to(root)) for path in changes))
print('Da sao luu ban cu:', backup)
for msg in optional_notes:
    print('LUU Y:', msg)
print('TIEP THEO: dotnet build ThiHocPhan_TrienKhai.sln')
