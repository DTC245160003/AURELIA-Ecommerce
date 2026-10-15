# AURELIA - Website thương mại điện tử siêu xe

## 1. Thông tin dự án
- Mã sinh viên: DTC245160003
- Đề tài: AURELIA - Website thương mại điện tử siêu xe
- Hệ điều hành: Ubuntu trên VMware
- Triển khai: Docker Compose

## 2. Công nghệ sử dụng
- ASP.NET Core MVC .NET 10
- MySQL 8.4
- phpMyAdmin 5
- Nginx Reverse Proxy
- Docker và Docker Compose
- Git và GitHub

## 3. Chức năng website
- Xem danh sách và chi tiết siêu xe
- Đăng ký, đăng nhập, đăng xuất
- Giỏ hàng và đặt hàng
- Quản lý sản phẩm, đơn hàng và khách hàng
- Phân quyền Admin, Staff, User
- Khóa và mở khóa tài khoản
- Giao diện sáng và tối

## 4. Kiến trúc triển khai

- Nginx: Reverse Proxy, tiếp nhận HTTP tại cổng 80.
- ASP.NET Core: chạy trong container web, cổng 8080.
- MySQL: lưu tài khoản, sản phẩm và đơn hàng.
- phpMyAdmin: công cụ quản trị MySQL.
- Docker Compose: quản lý toàn bộ các dịch vụ.

Luồng truy cập:
Trình duyệt -> Nginx -> ASP.NET Core -> MySQL

Các file cấu hình:
- Dockerfile
- compose.yaml
- compose.override.yaml
- nginx/default.conf

## 5. Hướng dẫn cài đặt

Yêu cầu: Ubuntu, Docker Engine, Docker Compose và Git.

Bước 1: Clone repository:
git clone https://github.com/DTC245160003/AURELIA-Ecommerce.git

Bước 2: Vào thư mục dự án:
cd AURELIA-Ecommerce

Bước 3: Tạo file môi trường:
cp .env.example .env

Bước 4: Thay mật khẩu mẫu trong .env bằng mật khẩu mạnh.

Bước 5: Khởi động:
sudo docker compose up -d --build

Bước 6: Kiểm tra:
sudo docker compose ps

## 6. Truy cập hệ thống

Website trên Ubuntu:
http://localhost/

Website từ Windows:
http://IP_UBUNTU/

phpMyAdmin trên Ubuntu:
http://127.0.0.1:8081/

Lưu ý: phpMyAdmin chỉ mở trên localhost Ubuntu.
Có thể dùng SSH port forwarding từ Windows.

## 7. Kiểm thử và nhật ký

Kiểm tra trạng thái container:
sudo docker compose ps

Kiểm tra website:
curl http://127.0.0.1/

Kiểm tra API sản phẩm:
curl http://127.0.0.1/api/vehicles

Xem nhật ký website:
sudo docker compose logs --tail=50 web

Xem nhật ký Nginx:
sudo docker compose logs --tail=50 nginx

## 8. Lưu trữ và bảo mật

- MySQL sử dụng Docker volume lưu dữ liệu.
- Data Protection Keys sử dụng Docker volume riêng.
- Website chạy bằng user không phải root.
- Mật khẩu MySQL được cấu hình trong .env.
- File .env không được đưa lên GitHub.
- Nginx có các HTTP security headers.
- MySQL và phpMyAdmin chỉ publish trên localhost Ubuntu.
- Không chạy docker compose down -v nếu muốn giữ dữ liệu.

## 9. Các giai đoạn triển khai

### Commit 1 - Website và Reverse Proxy
- ASP.NET Core MVC .NET 10
- MySQL 8.4 và phpMyAdmin
- Docker Compose
- Nginx Reverse Proxy và security headers

### Commit 2 - Monitoring (dự kiến)
- Prometheus và Grafana
- Thu thập metrics
- Dashboard giám sát hệ thống

### Commit 3 - Logging và Hardening (dự kiến)
- Loki và Promtail
- Truy vấn LogQL
- Kiểm thử và hoàn thiện bảo mật

## 10. Hướng dẫn demo Commit 1

1. Kiểm tra các container bằng Docker Compose.
2. Mở website AURELIA qua Nginx.
3. Kiểm tra đăng nhập và trang quản trị.
4. Kiểm tra danh sách xe và đơn hàng.
5. Mở phpMyAdmin để kiểm tra dữ liệu MySQL.
6. Kiểm tra HTTP security headers của Nginx.

Các giai đoạn 2 và 3 sẽ cập nhật README sau khi hoàn thành.
