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

### Commit 2 - Monitoring
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


## 11. Monitoring - Prometheus và Grafana

### 11.1. Mục đích

Hệ thống AURELIA sử dụng Prometheus và Grafana để giám sát hoạt động của website, các Docker container và cơ sở dữ liệu MySQL.

### 11.2. Các thành phần

- Prometheus 3.5.0: Thu thập và lưu trữ Metrics.
- Grafana 11.6.1: Hiển thị biểu đồ giám sát.
- Telegraf 1.36: Thu thập CPU và RAM của Docker container.
- MySQL Exporter 0.17.2: Thu thập Metrics từ MySQL.
- prometheus-net.AspNetCore: Cung cấp HTTP Metrics cho website ASP.NET Core.

### 11.3. Địa chỉ truy cập

Trên máy Ubuntu chạy Docker:

- Website: http://127.0.0.1
- Prometheus: http://127.0.0.1:9090
- Grafana: http://127.0.0.1:3000

Các dịch vụ Monitoring sử dụng kết nối localhost. Khi làm việc từ Windows qua VS Code Remote SSH, cần chuyển tiếp cổng tương ứng.

Mật khẩu Grafana được quản lý bằng biến GF_SECURITY_ADMIN_PASSWORD trong file .env.

### 11.4. Các nguồn dữ liệu Prometheus

Prometheus thu thập Metrics với chu kỳ 15 giây từ ba nguồn:

- aurelia-web: web:8080/metrics
- aurelia-docker: telegraf:9273/metrics
- aurelia-mysql: mysql-exporter:9104/metrics

Kiểm tra các Targets tại:

http://127.0.0.1:9090/targets

Trạng thái UP cho biết Prometheus đang thu thập thành công từ Target tương ứng.

Endpoint /metrics của website được sử dụng trong mạng Docker và bị Nginx chặn truy cập công khai.

### 11.5. Dashboard Grafana

Dashboard: AURELIA - System Monitoring

Bao gồm 5 biểu đồ:

1. Website Status: Trạng thái thu thập Metrics từ website.
2. Docker CPU Usage (%): Mức sử dụng CPU của các container.
3. Docker RAM Usage (MiB): Bộ nhớ RAM của các container.
4. MySQL Active Connections: Số kết nối MySQL đang hoạt động.
5. MySQL Queries per Second: Tốc độ xử lý câu lệnh của MySQL.

Grafana sử dụng Prometheus Data Source với URL nội bộ:

http://prometheus:9090

### 11.6. Grafana Provisioning

Cấu hình Monitoring được lưu tại:

- monitoring/prometheus/prometheus.yml
- monitoring/telegraf/telegraf.conf
- monitoring/grafana/provisioning/datasources/prometheus.yml
- monitoring/grafana/provisioning/dashboards/aurelia.yml
- monitoring/grafana/dashboards/aurelia-dashboard.json

Grafana đọc cấu hình Data Source và Dashboard từ các file này khi khởi động.

File monitoring/grafana/aurelia-dashboard.json được giữ lại để có thể Import thủ công.

Lưu ý: Khi triển khai với MySQL volume mới, cần tạo tài khoản metrics_exporter với quyền giám sát phù hợp và cấu hình MYSQL_EXPORTER_PASSWORD trước khi MySQL Exporter có thể thu thập dữ liệu.

### 11.7. Kiểm tra Monitoring

Kiểm tra các container:

sudo docker compose ps

Kiểm tra cấu hình Docker Compose:

sudo docker compose config -q

Kiểm tra nhật ký:

sudo docker compose logs --tail=50 grafana
sudo docker compose logs --tail=50 prometheus
sudo docker compose logs --tail=50 telegraf
sudo docker compose logs --tail=50 mysql-exporter

Mở Grafana, chọn Dashboards, vào thư mục AURELIA và mở AURELIA - System Monitoring.

Kiểm tra các biểu đồ hiển thị dữ liệu thực tế.

### 11.8. Lưu trữ và bảo mật

- Prometheus sử dụng Docker volume để lưu dữ liệu Metrics.
- Grafana sử dụng Docker volume để lưu cấu hình và dữ liệu.
- Mật khẩu được lưu trong .env, không đưa lên GitHub.
- Grafana và Prometheus chỉ publish trên localhost.
- MySQL Exporter sử dụng tài khoản giám sát riêng, không dùng tài khoản root.
- Không sử dụng docker compose down -v khi cần giữ dữ liệu.
