-- Chạy một lần trong database honganhshop (phpMyAdmin > SQL).
-- KHÔNG DROP hay ALTER products/orders/order_items.
CREATE TABLE IF NOT EXISTS users (
  id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  full_name VARCHAR(100) NOT NULL,
  email VARCHAR(255) NULL,
  phone VARCHAR(20) NULL,
  password_hash VARCHAR(512) NOT NULL,
  role ENUM('User','Staff','Admin') NOT NULL DEFAULT 'User',
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  failed_login_attempts INT UNSIGNED NOT NULL DEFAULT 0,
  locked_until_utc DATETIME(6) NULL,
  created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  CONSTRAINT chk_users_identifier CHECK (email IS NOT NULL OR phone IS NOT NULL),
  UNIQUE KEY uq_users_email (email),
  UNIQUE KEY uq_users_phone (phone)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
