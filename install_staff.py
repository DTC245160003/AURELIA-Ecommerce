#!/usr/bin/env python3
"""Install Admin/Staff role management without overwriting existing modules.
Execute at the project root after extracting AURELIA_Admin_Staff.zip.
"""
from datetime import datetime
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parent
program = root / 'src/HongAnhShop/Program.cs'
dashboard = root / 'src/HongAnhShop/Views/Admin/Index.cshtml'
customers = [
    root / 'src/HongAnhShop/Views/Admin/Customers.cshtml',
    root / 'src/HongAnhShop/Views/Admin/CustomerDetails.cshtml',
]

for path in (program, dashboard, *customers):
    if not path.is_file():
        sys.exit(f'THIEU FILE: {path}. Chua sua bat ky file nao.')

originals = {p: p.read_text(encoding='utf-8') for p in (program, dashboard, *customers)}
contents = dict(originals)

# Existing customer module must keep rejecting stale cookies (role changes).
p = contents[program]
if 'options.Events.OnValidatePrincipal' not in p or 'GetSecurityStateAsync' not in p:
    sys.exit('CHUA CO KIEM TRA COOKIE TU MYSQL. Hay cai/test Customers module truoc. Khong sua file nao.')
needle = 'builder.Services.AddScoped<AdminCustomerRepository>();'
if needle not in p:
    sys.exit('Khong tim thay AdminCustomerRepository trong Program.cs. Khong sua file nao.')
if 'builder.Services.AddScoped<AdminStaffRepository>();' not in p:
    if p.count(needle) != 1:
        sys.exit('DI anchor khong duy nhat. Khong sua file nao.')
    p = p.replace(needle, needle + '\nbuilder.Services.AddScoped<AdminStaffRepository>();')
contents[program] = p

p = contents[dashboard]
if 'href="/Admin/Staff"' not in p:
    staff_nav = re.compile(r'<span\s+class="nav-link disabled">\s*◇\s*Phân quyền nhân viên\s*</span>', re.U)
    updated, count = staff_nav.subn('<a class="nav-link" href="/Admin/Staff">\n                        ◇ Phân quyền nhân viên\n                    </a>', p)
    if count != 1:
        sys.exit('Khong nhan dien duoc menu Staff cua Dashboard. Khong sua file nao.')
    p = updated
# Replace status/action in correct staff card only, not customer or orders.
card_re = re.compile(r'(<h3>Phân quyền nhân viên</h3>[\s\S]*?)(<span class="card-status">CHỈ DÀNH CHO ADMIN</span>)', re.U)
if 'href="/Admin/Staff">Mở phân quyền nhân viên' not in p:
    p, count = card_re.subn(r'\1<div class="card-status">ĐANG HOẠT ĐỘNG · CHỈ ADMIN</div>\n                        <div><a class="card-action" href="/Admin/Staff">Mở phân quyền nhân viên →</a></div>', p)
    if count != 1:
        sys.exit('Khong nhan dien duoc the Staff tren Dashboard. Khong sua file nao.')
p = p.replace('Phân quyền nhân viên sẽ được triển khai tiếp.', 'Chỉ Admin có quyền phân quyền nhân viên.')
p = p.replace('nhân viên đang chờ triển khai.', 'nhân viên đã được kết nối MySQL.')
contents[dashboard] = p

# Enable consistent sidebar navigation from previously installed Customer views.
old = '@if (User.IsInRole("Admin")) { <div class="soon">◇ Phân quyền nhân viên — sắp có</div> }'
new = '@if (User.IsInRole("Admin")) { <a href="/Admin/Staff">◇ Phân quyền nhân viên</a> }'
for path in customers:
    p = contents[path]
    if 'href="/Admin/Staff"' not in p:
        if p.count(old) != 1:
            sys.exit(f'Khong tim thay menu Staff mac dinh trong {path.name}. Khong sua file nao.')
        p = p.replace(old, new)
    contents[path] = p

changes = [path for path in originals if contents[path] != originals[path]]
if not changes:
    print('OK: module da duoc cai. Khong can thay doi them.')
    sys.exit(0)

backup = root / 'aurelia-backups' / ('staff-' + datetime.now().strftime('%Y%m%d-%H%M%S-%f'))
backup.mkdir(parents=True, exist_ok=False)
for file in changes:
    (backup / ('Program.cs' if file == program else 'Admin.Index.cshtml' if file == dashboard else file.name)).write_text(originals[file], encoding='utf-8')

try:
    for path in changes:
        path.write_text(contents[path], encoding='utf-8')
except Exception:
    for path in changes:
        path.write_text(originals[path], encoding='utf-8')
    raise

print('OK: Da dang ky AdminStaffRepository va kich hoat Staff menu.')
print('Da sao luu tai:', backup)
print('Tiep theo: dotnet build ThiHocPhan_TrienKhai.sln')
