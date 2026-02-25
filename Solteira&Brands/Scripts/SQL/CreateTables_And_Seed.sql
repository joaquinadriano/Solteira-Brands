-- ============================================================================
-- Solteira & Brands — SINGLE SQL SCRIPT (update this file as schema changes)
-- Database: SolteiraBrands (MySQL / MariaDB)
-- Run in HeidiSQL: open this file in Query tab, then Execute (F9) or Run (Ctrl+F9).
-- Idempotent: safe to run multiple times.
--
-- Current schema:
--   tbl_roles, tbl_users (no first_name/last_name; auth_provider),
--   tbl_verification_tokens, tbl_images
-- Trial accounts: SystemAdmin, Manager, Staff, Buyer (password: Password123@)
-- After first run: visit /Account/SeedTrialPasswords to set real password hashes.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. tbl_roles
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_roles (
    id              INT             NOT NULL AUTO_INCREMENT,
    name            VARCHAR(50)     NOT NULL,
    description     VARCHAR(255)    NULL,
    created_at      DATETIME        NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_tbl_roles_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 2. tbl_users (username only; no first_name/last_name)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_users (
    id              INT             NOT NULL AUTO_INCREMENT,
    role_id         INT             NOT NULL,
    email           VARCHAR(150)    NOT NULL,
    username        VARCHAR(150)    NOT NULL,
    auth_provider   VARCHAR(50)    NOT NULL DEFAULT 'LOCAL',
    password_hash   VARCHAR(255)   NOT NULL,
    password_salt   VARCHAR(255)   NOT NULL,
    is_verified     TINYINT(1)      NOT NULL DEFAULT 0,
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    created_at      DATETIME        NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_tbl_users_email (email),
    UNIQUE KEY uk_tbl_users_username (username),
    CONSTRAINT fk_tbl_users_role
        FOREIGN KEY (role_id)
        REFERENCES tbl_roles (id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 3. tbl_verification_tokens (email verification + password reset)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_verification_tokens (
    id              INT             NOT NULL AUTO_INCREMENT,
    user_id         INT             NOT NULL,
    token_value     VARCHAR(255)    NOT NULL,
    token_type      VARCHAR(50)     NOT NULL,
    expires_at      DATETIME       NOT NULL,
    is_used         TINYINT(1)     NOT NULL DEFAULT 0,
    created_at      DATETIME       NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_user_id (user_id),
    CONSTRAINT fk_verification_user
        FOREIGN KEY (user_id)
        REFERENCES tbl_users (id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tbl_images (
    id              INT             NOT NULL AUTO_INCREMENT,
    image_path      VARCHAR(255)    NOT NULL,
    image_file_name VARCHAR(255)   NOT NULL,
    created_at      DATETIME       NOT NULL,
    updated_at      DATETIME       NOT NULL,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5. tbl_products (storefront products; soft delete via is_archived)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_products (
    id              INT             NOT NULL AUTO_INCREMENT,
    name            VARCHAR(150)    NOT NULL,
    category        VARCHAR(100)    NOT NULL,
    description     VARCHAR(255)    NULL,
    price           DECIMAL(10,2)   NOT NULL,
    material        VARCHAR(150)    NULL,
    stones          VARCHAR(255)    NULL,
    weight          VARCHAR(50)     NULL,
    length          VARCHAR(50)     NULL,
    stock_quantity  INT             NOT NULL DEFAULT 0,
    image_id        INT             NULL,
    is_best_seller  TINYINT(1)      NOT NULL DEFAULT 0,
    is_archived     TINYINT(1)      NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL,
    updated_at      DATETIME        NOT NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_tbl_products_image
        FOREIGN KEY (image_id)
        REFERENCES tbl_images (id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- ORDER LIFECYCLE — Three status layers (order status is NEVER manually set)
-- ----------------------------------------------------------------------------
-- 1. tbl_orders.order_status: Pending | Approved | Rejected | Awaiting Payment Verification |
--    Paid | Layaway Active | In Delivery | Ready for Meetup | Completed | Cancelled
--    delivery_type on order: COURIER | MEETUP (set at checkout; payment verification uses it)
-- 2. tbl_payments.payment_status: SUBMITTED | VERIFIED | REJECTED
-- 3. tbl_deliveries.delivery_status: PENDING | IN_TRANSIT | DELIVERED (shipments only; order_status = Paid required)
-- 4. tbl_meetup_outcomes: meetup completion (Ready for Meetup → Completed/No Show/Cancelled)
-- ----------------------------------------------------------------------------

-- ----------------------------------------------------------------------------
-- 5b. tbl_orders (buyer checkout orders)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_orders (
    id                      INT             NOT NULL AUTO_INCREMENT,
    user_id                 INT             NOT NULL,
    order_number            VARCHAR(50)     NOT NULL,
    customer_name           VARCHAR(150)    NOT NULL,
    contact_number          VARCHAR(50)     NOT NULL,
    delivery_address        VARCHAR(500)    NOT NULL,
    payment_method          VARCHAR(100)    NOT NULL,
    payment_type            VARCHAR(50)     NULL,
    total_amount            DECIMAL(12,2)   NOT NULL,
    remaining_balance       DECIMAL(12,2)   NOT NULL DEFAULT 0,
    order_status            VARCHAR(50)     NOT NULL DEFAULT 'Pending',
    delivery_type           VARCHAR(50)     NULL DEFAULT 'COURIER',
    rejection_reason        VARCHAR(500)    NULL,
    created_at              DATETIME        NOT NULL,
    last_status_update_at   DATETIME        NULL,
    PRIMARY KEY (id),
    INDEX idx_tbl_orders_user_id (user_id),
    INDEX idx_tbl_orders_created_at (created_at),
    CONSTRAINT fk_tbl_orders_user
        FOREIGN KEY (user_id)
        REFERENCES tbl_users (id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5b2. tbl_payments (payment_status: SUBMITTED | VERIFIED | REJECTED only)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_payments (
    id                  INT             NOT NULL AUTO_INCREMENT,
    order_id            INT             NOT NULL,
    amount              DECIMAL(12,2)   NOT NULL,
    payment_status      VARCHAR(50)     NOT NULL DEFAULT 'SUBMITTED',
    rejection_reason    VARCHAR(500)    NULL,
    proof_image_id      INT             NULL,
    reference_number    VARCHAR(100)    NULL,
    meetup_datetime     DATETIME        NULL,
    verified_by_user_id INT             NULL,
    created_at          DATETIME        NOT NULL,
    verified_at         DATETIME        NULL,
    allow_resubmit      TINYINT(1)      NOT NULL DEFAULT 1 COMMENT '1=buyer may resubmit after reject, 0=final reject',
    PRIMARY KEY (id),
    INDEX idx_tbl_payments_order_id (order_id),
    CONSTRAINT fk_tbl_payments_order
        FOREIGN KEY (order_id)
        REFERENCES tbl_orders (id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_payments_proof_image
        FOREIGN KEY (proof_image_id)
        REFERENCES tbl_images (id)
        ON DELETE SET NULL
        ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_payments_verified_by
        FOREIGN KEY (verified_by_user_id)
        REFERENCES tbl_users (id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5b3. tbl_deliveries (delivery_status: PENDING | IN_TRANSIT | DELIVERED only)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_deliveries (
    id                  INT             NOT NULL AUTO_INCREMENT,
    order_id            INT             NOT NULL,
    delivery_type       VARCHAR(50)     NOT NULL,
    tracking_number     VARCHAR(200)    NULL,
    meetup_datetime     DATETIME        NULL,
    delivery_status     VARCHAR(50)     NOT NULL DEFAULT 'PENDING',
    created_at          DATETIME        NOT NULL,
    updated_at          DATETIME        NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_tbl_deliveries_order_id (order_id),
    CONSTRAINT fk_tbl_deliveries_order
        FOREIGN KEY (order_id)
        REFERENCES tbl_orders (id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5b4. tbl_meetup_outcomes (Ready for Meetup → Completed with receipt | No Show | Cancelled)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_meetup_outcomes (
    id                  INT             NOT NULL AUTO_INCREMENT,
    order_id            INT             NOT NULL,
    status              VARCHAR(50)     NOT NULL,
    receipt_image_id    INT             NULL,
    created_at          DATETIME        NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_meetup_outcomes_order_id (order_id),
    CONSTRAINT fk_meetup_outcomes_order
        FOREIGN KEY (order_id)
        REFERENCES tbl_orders (id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT fk_meetup_outcomes_receipt
        FOREIGN KEY (receipt_image_id)
        REFERENCES tbl_images (id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5c. tbl_order_items (line items per order)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_order_items (
    id              INT             NOT NULL AUTO_INCREMENT,
    order_id        INT             NOT NULL,
    product_id      INT             NULL,
    product_name    VARCHAR(200)    NOT NULL,
    quantity        INT             NOT NULL,
    unit_price      DECIMAL(10,2)   NOT NULL,
    image_url       VARCHAR(500)    NULL,
    PRIMARY KEY (id),
    INDEX idx_tbl_order_items_order_id (order_id),
    CONSTRAINT fk_tbl_order_items_order
        FOREIGN KEY (order_id)
        REFERENCES tbl_orders (id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_order_items_product
        FOREIGN KEY (product_id)
        REFERENCES tbl_products (id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5c2. tbl_payment_settings (one active per payment_type: GCASH | BANK_TRANSFER)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_payment_settings (
    id                  INT             NOT NULL AUTO_INCREMENT,
    payment_type        VARCHAR(50)     NOT NULL,
    account_name        VARCHAR(150)    NULL,
    account_number     VARCHAR(100)    NULL,
    qr_image_id         INT             NULL,
    is_active           TINYINT(1)      NOT NULL DEFAULT 1,
    created_at          DATETIME        NOT NULL,
    updated_at          DATETIME        NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_payment_settings_type_active (payment_type, is_active),
    CONSTRAINT fk_payment_settings_qr_image
        FOREIGN KEY (qr_image_id)
        REFERENCES tbl_images (id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5c3. tbl_meetup_settings (one active location)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_meetup_settings (
    id                  INT             NOT NULL AUTO_INCREMENT,
    location_name       VARCHAR(150)    NOT NULL,
    address             VARCHAR(500)    NOT NULL,
    is_active           TINYINT(1)      NOT NULL DEFAULT 1,
    created_at          DATETIME        NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_meetup_settings_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5c4. tbl_layaway_settings (single config: downpayment %, months)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_layaway_settings (
    id                      INT             NOT NULL AUTO_INCREMENT,
    downpayment_percentage  DECIMAL(5,2)     NOT NULL DEFAULT 30,
    months_allowed          INT             NOT NULL DEFAULT 3,
    created_at              DATETIME        NOT NULL,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5d. tbl_activity_logs (audit trail for admin/manager/staff actions)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_activity_logs (
    id              INT             NOT NULL AUTO_INCREMENT,
    user_id         INT             NOT NULL,
    username        VARCHAR(150)    NOT NULL,
    role_name       VARCHAR(50)     NOT NULL,
    action          VARCHAR(100)    NOT NULL,
    module          VARCHAR(50)     NOT NULL,
    entity_id       INT             NULL,
    details         VARCHAR(500)    NULL,
    created_at      DATETIME        NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_tbl_activity_logs_created_at (created_at),
    INDEX idx_tbl_activity_logs_user_id (user_id),
    CONSTRAINT fk_tbl_activity_logs_user
        FOREIGN KEY (user_id)
        REFERENCES tbl_users (id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 6. Idempotent changes for EXISTING databases (run each time; no-op if already applied)
-- ----------------------------------------------------------------------------
SET @dbname := DATABASE();

-- Drop first_name from tbl_users if it exists (you already did this)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_users' AND COLUMN_NAME = 'first_name');
SET @ddl := IF(@col_exists > 0, 'ALTER TABLE tbl_users DROP COLUMN first_name;', 'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_users' AND COLUMN_NAME = 'last_name');
SET @ddl := IF(@col_exists > 0, 'ALTER TABLE tbl_users DROP COLUMN last_name;', 'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add auth_provider to tbl_users if not exists (LOCAL | GOOGLE)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_users' AND COLUMN_NAME = 'auth_provider');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_users ADD COLUMN auth_provider VARCHAR(50) NOT NULL DEFAULT ''LOCAL'' AFTER username;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add avatar_image_id to tbl_users if not exists (profile picture → tbl_images)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_users' AND COLUMN_NAME = 'avatar_image_id');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_users ADD COLUMN avatar_image_id INT NULL AFTER created_at, ADD CONSTRAINT fk_tbl_users_avatar_image FOREIGN KEY (avatar_image_id) REFERENCES tbl_images(id) ON DELETE SET NULL ON UPDATE CASCADE;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add payment_type to tbl_orders (FULL | LAYAWAY | CASH_MEETUP)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_orders' AND COLUMN_NAME = 'payment_type');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_orders ADD COLUMN payment_type VARCHAR(50) NULL AFTER payment_method;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add remaining_balance to tbl_orders
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_orders' AND COLUMN_NAME = 'remaining_balance');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_orders ADD COLUMN remaining_balance DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER total_amount;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add last_status_update_at to tbl_orders
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_orders' AND COLUMN_NAME = 'last_status_update_at');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_orders ADD COLUMN last_status_update_at DATETIME NULL AFTER created_at;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add rejection_reason to tbl_orders (admin reject order)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_orders' AND COLUMN_NAME = 'rejection_reason');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_orders ADD COLUMN rejection_reason VARCHAR(500) NULL AFTER order_status;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add resubmit_window_days to tbl_layaway_settings (days allowed to resubmit after rejection; default 3)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_layaway_settings' AND COLUMN_NAME = 'resubmit_window_days');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_layaway_settings ADD COLUMN resubmit_window_days INT NOT NULL DEFAULT 3 AFTER months_allowed;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
-- Add grace_period_days to tbl_layaway_settings (days past due before marking Overdue; default 0)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_layaway_settings' AND COLUMN_NAME = 'grace_period_days');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_layaway_settings ADD COLUMN grace_period_days INT NOT NULL DEFAULT 0 AFTER resubmit_window_days;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add reference_number and meetup_datetime to tbl_payments (for proof-based and meetup submissions)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_payments' AND COLUMN_NAME = 'reference_number');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_payments ADD COLUMN reference_number VARCHAR(100) NULL AFTER proof_image_id;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_payments' AND COLUMN_NAME = 'meetup_datetime');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_payments ADD COLUMN meetup_datetime DATETIME NULL AFTER reference_number;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add allow_resubmit to tbl_payments (admin checkbox: allow buyer to resubmit after reject)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_payments' AND COLUMN_NAME = 'allow_resubmit');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_payments ADD COLUMN allow_resubmit TINYINT(1) NOT NULL DEFAULT 1 COMMENT ''1=buyer may resubmit, 0=final reject'' AFTER verified_at;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add delivery_type to tbl_orders (COURIER | MEETUP; set at checkout; payment verification uses it)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_orders' AND COLUMN_NAME = 'delivery_type');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_orders ADD COLUMN delivery_type VARCHAR(50) NULL DEFAULT ''COURIER'' AFTER order_status;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add courier, dispatch_date, eta to tbl_deliveries (shipments: editable when PENDING)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_deliveries' AND COLUMN_NAME = 'courier');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_deliveries ADD COLUMN courier VARCHAR(100) NULL AFTER delivery_type;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_deliveries' AND COLUMN_NAME = 'dispatch_date');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_deliveries ADD COLUMN dispatch_date DATETIME NULL AFTER courier;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'tbl_deliveries' AND COLUMN_NAME = 'eta');
SET @ddl := IF(@col_exists = 0,
    'ALTER TABLE tbl_deliveries ADD COLUMN eta DATETIME NULL AFTER dispatch_date;',
    'SELECT 1;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ----------------------------------------------------------------------------
-- 7. Seed roles
-- ----------------------------------------------------------------------------
INSERT INTO tbl_roles (name, description, created_at) VALUES
('ADMIN',   'Technical authority',    NOW()),
('MANAGER', 'Business authority',    NOW()),
('STAFF',   'Operational support',   NOW()),
('BUYER',   'Customer role',         NOW())
ON DUPLICATE KEY UPDATE name = VALUES(name);

-- ----------------------------------------------------------------------------
-- 8. Trial accounts (SystemAdmin, Manager, Staff, Buyer — password: Password123@) !!!!!!!!!!!!!!
--    Hash/salt = SEEDME until you visit /Account/SeedTrialPasswords
-- ----------------------------------------------------------------------------
INSERT INTO tbl_users (role_id, email, username, password_hash, password_salt, is_verified, is_active, created_at)
SELECT r.id, 'systemadmin@solteira.local', 'SystemAdmin', 'SEEDME', 'SEEDME', 1, 1, NOW() FROM tbl_roles r WHERE r.name = 'ADMIN'  LIMIT 1
ON DUPLICATE KEY UPDATE email = VALUES(email);

INSERT INTO tbl_users (role_id, email, username, password_hash, password_salt, is_verified, is_active, created_at)
SELECT r.id, 'manager@solteira.local',     'Manager',   'SEEDME', 'SEEDME', 1, 1, NOW() FROM tbl_roles r WHERE r.name = 'MANAGER' LIMIT 1
ON DUPLICATE KEY UPDATE email = VALUES(email);

INSERT INTO tbl_users (role_id, email, username, password_hash, password_salt, is_verified, is_active, created_at)
SELECT r.id, 'staff@solteira.local',       'Staff',     'SEEDME', 'SEEDME', 1, 1, NOW() FROM tbl_roles r WHERE r.name = 'STAFF'   LIMIT 1
ON DUPLICATE KEY UPDATE email = VALUES(email);

INSERT INTO tbl_users (role_id, email, username, password_hash, password_salt, is_verified, is_active, created_at)
SELECT r.id, 'buyer@solteira.local',      'Buyer',     'SEEDME', 'SEEDME', 1, 1, NOW() FROM tbl_roles r WHERE r.name = 'BUYER'   LIMIT 1
ON DUPLICATE KEY UPDATE email = VALUES(email);

-- ----------------------------------------------------------------------------
-- 9. Payment settings (GCASH and BANK_TRANSFER) – seed if none active
--    Ensures GET payment settings returns at least one option for buyer payment page
-- ----------------------------------------------------------------------------
INSERT INTO tbl_payment_settings (payment_type, account_name, account_number, qr_image_id, is_active, created_at, updated_at)
SELECT 'GCASH', 'GCash', '', NULL, 1, NOW(), NOW() FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM tbl_payment_settings WHERE payment_type = 'GCASH' AND is_active = 1);

INSERT INTO tbl_payment_settings (payment_type, account_name, account_number, qr_image_id, is_active, created_at, updated_at)
SELECT 'BANK_TRANSFER', 'Bank Transfer', '', NULL, 1, NOW(), NOW() FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM tbl_payment_settings WHERE payment_type = 'BANK_TRANSFER' AND is_active = 1);

-- ============================================================================
-- Next step: Run the app and open in browser:
--   https://localhost:YOUR_PORT/Account/SeedTrialPasswords
-- ============================================================================
