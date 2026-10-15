-- AURELIA: nullable user_id for account-bound orders.
-- Safe for existing guest orders: existing rows remain with user_id = NULL.
-- Run as MySQL root against database honganhshop, after a full database backup.
USE honganhshop;

SET @has_user_id := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'orders' AND COLUMN_NAME = 'user_id'
);
SET @ddl := IF(@has_user_id = 0,
  'ALTER TABLE orders ADD COLUMN user_id INT NULL AFTER id',
  'SELECT ''user_id column already exists'' AS message');
PREPARE aurelia_migration FROM @ddl;
EXECUTE aurelia_migration;
DEALLOCATE PREPARE aurelia_migration;

-- Detect a foreign key regardless of its name.
SET @has_order_user_fk := (
  SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'orders'
    AND COLUMN_NAME = 'user_id' AND REFERENCED_TABLE_NAME = 'users'
    AND REFERENCED_COLUMN_NAME = 'id'
);
SET @ddl := IF(@has_order_user_fk = 0,
  'ALTER TABLE orders ADD CONSTRAINT fk_aurelia_orders_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT',
  'SELECT ''orders-user foreign key already exists'' AS message');
PREPARE aurelia_migration FROM @ddl;
EXECUTE aurelia_migration;
DEALLOCATE PREPARE aurelia_migration;

SET @has_order_user_index := (
  SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'orders'
    AND INDEX_NAME = 'idx_aurelia_orders_user_created'
);
SET @ddl := IF(@has_order_user_index = 0,
  'CREATE INDEX idx_aurelia_orders_user_created ON orders(user_id, created_at, id)',
  'SELECT ''user order index already exists'' AS message');
PREPARE aurelia_migration FROM @ddl;
EXECUTE aurelia_migration;
DEALLOCATE PREPARE aurelia_migration;

-- shop_app already inserts orders and reads users; ensure history read access.
GRANT SELECT ON honganhshop.orders TO 'shop_app'@'%';
GRANT SELECT ON honganhshop.order_items TO 'shop_app'@'%';
GRANT SELECT ON honganhshop.users TO 'shop_app'@'%';

SELECT COUNT(*) AS total_orders,
       SUM(user_id IS NOT NULL) AS orders_linked_to_users,
       SUM(user_id IS NULL) AS guest_or_legacy_orders
FROM orders;
