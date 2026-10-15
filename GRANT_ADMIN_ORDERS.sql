-- Run once with MySQL root, not with shop_app.
-- Minimum new privileges needed for viewing orders and changing their statuses.
GRANT SELECT, UPDATE ON honganhshop.orders TO 'shop_app'@'%';
GRANT SELECT ON honganhshop.order_items TO 'shop_app'@'%';
