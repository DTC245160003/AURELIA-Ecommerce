-- AURELIA / HongAnhShop - chuyển danh mục hàng điện tử sang siêu xe
-- Dùng 1 lần trong phpMyAdmin -> database honganhshop -> SQL.
-- BẮT BUỘC: Export (sao lưu) toàn bộ database trước khi thực hiện.
-- Giữ nguyên dữ liệu orders/order_items và IDs sản phẩm cũ.
-- Dữ liệu mẫu 12 xe trích đúng từ Home AURELIA hiện tại; giá không phải báo giá thực tế.

USE `honganhshop`;

-- 1. Lưu bản sao danh sách sản phẩm cũ, không ghi đè nếu chạy lại.
CREATE TABLE IF NOT EXISTS `products_before_aurelia` LIKE `products`;
INSERT IGNORE INTO `products_before_aurelia`
(`id`, `name`, `category`, `description`, `price`, `image_url`, `stock`, `created_at`)
SELECT `id`, `name`, `category`, `description`, `price`, `image_url`, `stock`, `created_at`
FROM `products` WHERE `id` IN (1,2,3,4,5,6);

-- 2. Mở rộng các cột tiền để đủ lưu giá xe hàng chục/hàng trăm tỷ VNĐ.
ALTER TABLE `products` MODIFY COLUMN `price` DECIMAL(18,2) NOT NULL;
ALTER TABLE `orders` MODIFY COLUMN `total_amount` DECIMAL(18,2) NOT NULL;
ALTER TABLE `order_items` MODIFY COLUMN `unit_price` DECIMAL(18,2) NOT NULL;

-- 3. Tạo cột trạng thái nếu chưa tồn tại (MySQL 8.4).
SET @has_column := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'products' AND COLUMN_NAME = 'is_active'
);
SET @ddl := IF(@has_column=0,
  'ALTER TABLE `products` ADD COLUMN `is_active` TINYINT(1) NOT NULL DEFAULT 1',
  'SELECT ''is_active da ton tai'' AS note');
PREPARE aurelia_add_column FROM @ddl;
EXECUTE aurelia_add_column;
DEALLOCATE PREPARE aurelia_add_column;

-- 4. Ẩn điện tử cũ. Không UPDATE tên/giá theo ID vì đơn hàng cũ đang tham chiếu.
UPDATE `products` SET `is_active` = 0
WHERE `id` IN (1,2,3,4,5,6)
  AND `name` IN ('Laptop Office 14','Tai nghe Bluetooth','Chuột không dây',
                 'Bàn phím cơ','Màn hình 24 inch','Balo laptop');

-- 5. Thêm 12 xe, không trùng tên khi lỡ chạy SQL thêm lần nữa.
START TRANSACTION;
INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Ferrari SF90 Stradale (2024)', 'Hypercar', 'Ferrari SF90 Stradale 2024. V8 4.0L Hybrid; 1.000 mã lực; Ly hợp kép 8 cấp. Địa điểm: TP. Hồ Chí Minh. Hình ảnh và giá bán chỉ mang tính minh họa.', 96250000000, '/images/cars/silver.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Ferrari SF90 Stradale (2024)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Lamborghini Revuelto (2023)', 'Hypercar', 'Lamborghini Revuelto 2023. V12 6.5L Hybrid; 1.015 mã lực; Ly hợp kép 8 cấp. Địa điểm: Hà Nội. Hình ảnh và giá bán chỉ mang tính minh họa.', 105000000000, '/images/cars/blue.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Lamborghini Revuelto (2023)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Aston Martin DB12 (2024)', 'Grand Tourer', 'Aston Martin DB12 2024. V8 4.0L Twin-Turbo; 680 mã lực; Tự động 8 cấp. Địa điểm: Đà Nẵng. Hình ảnh và giá bán chỉ mang tính minh họa.', 62000000000, '/images/cars/red.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Aston Martin DB12 (2024)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'McLaren 765LT (2023)', 'Xe đường đua', 'McLaren 765LT 2023. V8 4.0L Twin-Turbo; 765 mã lực; SSG 7 cấp. Địa điểm: Quốc tế. Hình ảnh và giá bán chỉ mang tính minh họa.', 143750000000, '/images/cars/grey.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'McLaren 765LT (2023)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Bugatti Chiron Super Sport (2022)', 'Phiên bản giới hạn', 'Bugatti Chiron Super Sport 2022. W16 8.0L Quad-Turbo; 1.600 mã lực; Ly hợp kép 7 cấp. Địa điểm: Quốc tế. Hình ảnh và giá bán chỉ mang tính minh họa.', 210000000000, '/images/cars/hero-1.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Bugatti Chiron Super Sport (2022)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Porsche 911 GT3 RS (2024)', 'Xe đường đua', 'Porsche 911 GT3 RS 2024. Boxer 6 4.0L; 525 mã lực; PDK 7 cấp. Địa điểm: TP. Hồ Chí Minh. Hình ảnh và giá bán chỉ mang tính minh họa.', 22500000000, '/images/cars/silver.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Porsche 911 GT3 RS (2024)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Bentley Continental GT Speed (2024)', 'Grand Tourer', 'Bentley Continental GT Speed 2024. W12 6.0L Twin-Turbo; 659 mã lực; Ly hợp kép 8 cấp. Địa điểm: Hà Nội. Hình ảnh và giá bán chỉ mang tính minh họa.', 28900000000, '/images/cars/hero-2.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Bentley Continental GT Speed (2024)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Rolls-Royce Spectre (2024)', 'Grand Tourer', 'Rolls-Royce Spectre 2024. Điện, hai mô-tơ; 585 mã lực; Một cấp. Địa điểm: TP. Hồ Chí Minh. Hình ảnh và giá bán chỉ mang tính minh họa.', 48000000000, '/images/cars/hero-2.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Rolls-Royce Spectre (2024)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Ferrari 296 GTB (2023)', 'Grand Tourer', 'Ferrari 296 GTB 2023. V6 3.0L Hybrid; 830 mã lực; Ly hợp kép 8 cấp. Địa điểm: Đà Nẵng. Hình ảnh và giá bán chỉ mang tính minh họa.', 26800000000, '/images/cars/red.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Ferrari 296 GTB (2023)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Lamborghini Huracán STO (2023)', 'Xe đường đua', 'Lamborghini Huracán STO 2023. V10 5.2L; 640 mã lực; Ly hợp kép 7 cấp. Địa điểm: Hà Nội. Hình ảnh và giá bán chỉ mang tính minh họa.', 31500000000, '/images/cars/grey.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Lamborghini Huracán STO (2023)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'McLaren Senna (2022)', 'Phiên bản giới hạn', 'McLaren Senna 2022. V8 4.0L Twin-Turbo; 800 mã lực; SSG 7 cấp. Địa điểm: Quốc tế. Hình ảnh và giá bán chỉ mang tính minh họa.', 82000000000, '/images/cars/blue.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'McLaren Senna (2022)');

INSERT INTO `products` (`name`,`category`,`description`,`price`,`image_url`,`stock`,`is_active`)
SELECT 'Porsche 918 Spyder (2015)', 'Phiên bản giới hạn', 'Porsche 918 Spyder 2015. V8 4.6L Hybrid; 887 mã lực; PDK 7 cấp. Địa điểm: TP. Hồ Chí Minh. Hình ảnh và giá bán chỉ mang tính minh họa.', 72000000000, '/images/cars/silver.jpg', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM `products` WHERE `name` = 'Porsche 918 Spyder (2015)');

COMMIT;

-- 6. Kiểm tra nhanh sau khi chạy:
SELECT `id`,`name`,`category`,`price`,`image_url`,`stock`,`is_active`
FROM `products` ORDER BY `is_active` DESC, `id` ASC;
-- Kỳ vọng: 12 mẫu xe với is_active=1; 6 hàng điện tử cũ is_active=0.
-- NHỚ: Trong ProductRepository.GetAllAsync(), bổ sung WHERE is_active = 1.
