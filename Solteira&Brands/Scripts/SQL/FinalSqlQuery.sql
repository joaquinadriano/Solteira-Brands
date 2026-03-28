-- ============================================================================
-- Solteira & Brands — FINAL CANONICAL SCHEMA (one copy-paste for new databases)
-- Database: SolteiraBrands (MySQL / MariaDB, utf8mb4)
--
-- VARCHAR lengths match Entity Framework Maps + ProductFieldConstraints.cs:
--   Products: name 30, spec fields 50, category 100, description 255
--   Orders/checkout: delivery_address 100 (CreateOrderRequest), customer_name 150,
--   contact_number 50, payment_method 100, status/type fields 50, reasons 500
--
-- For incremental upgrades on existing DBs, keep using:
--   Scripts/SQL/CreateTables_And_Seed.sql (idempotent ALTERs + seeds).
--
-- Idempotent: CREATE TABLE IF NOT EXISTS + INSERT ... ON DUPLICATE / WHERE NOT EXISTS.
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
-- 2. tbl_images (before tbl_users avatar FK and tbl_products image FK)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_images (
    id                INT             NOT NULL AUTO_INCREMENT,
    image_path        VARCHAR(255)    NOT NULL,
    image_file_name   VARCHAR(255)    NOT NULL,
    created_at        DATETIME        NOT NULL,
    updated_at        DATETIME        NOT NULL,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 3. tbl_users
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_users (
    id                    INT             NOT NULL AUTO_INCREMENT,
    role_id               INT             NOT NULL,
    email                 VARCHAR(150)    NOT NULL,
    username              VARCHAR(150)    NOT NULL,
    auth_provider         VARCHAR(50)     NOT NULL DEFAULT 'LOCAL',
    first_name            VARCHAR(50)     NOT NULL,
    last_name             VARCHAR(50)     NOT NULL,
    password_hash         VARCHAR(255)    NOT NULL,
    password_salt         VARCHAR(255)    NOT NULL,
    is_verified           TINYINT(1)      NOT NULL DEFAULT 0,
    is_active             TINYINT(1)      NOT NULL DEFAULT 1,
    username_changed_at   DATETIME        NULL,
    avatar_image_id       INT             NULL,
    accepted_terms        TINYINT(1)      NOT NULL DEFAULT 0,
    accepted_at           DATETIME        NULL,
    created_at            DATETIME        NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_tbl_users_email (email),
    UNIQUE KEY uk_tbl_users_username (username),
    CONSTRAINT fk_tbl_users_role
        FOREIGN KEY (role_id) REFERENCES tbl_roles (id)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_users_avatar_image
        FOREIGN KEY (avatar_image_id) REFERENCES tbl_images (id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 4. tbl_verification_tokens
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_verification_tokens (
    id              INT             NOT NULL AUTO_INCREMENT,
    user_id         INT             NOT NULL,
    token_value     VARCHAR(255)    NOT NULL,
    token_type      VARCHAR(50)     NOT NULL,
    expires_at      DATETIME        NOT NULL,
    is_used         TINYINT(1)      NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_user_id (user_id),
    CONSTRAINT fk_verification_user
        FOREIGN KEY (user_id) REFERENCES tbl_users (id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 5. tbl_products (name/spec limits = ProductFieldConstraints)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_products (
    id              INT             NOT NULL AUTO_INCREMENT,
    name            VARCHAR(30)     NOT NULL,
    category        VARCHAR(100)    NOT NULL,
    description     VARCHAR(255)    NULL,
    price           DECIMAL(10,2)   NOT NULL,
    material        VARCHAR(50)     NULL,
    stones          VARCHAR(50)     NULL,
    weight          VARCHAR(50)     NULL,
    length          VARCHAR(50)     NULL,
    stock_quantity  INT             NOT NULL DEFAULT 0,
    initial_stock   INT             NULL COMMENT 'Original quantity when product was created',
    image_id        INT             NULL,
    is_best_seller  TINYINT(1)      NOT NULL DEFAULT 0,
    is_featured     TINYINT(1)      NOT NULL DEFAULT 0,
    is_archived     TINYINT(1)      NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL,
    updated_at      DATETIME        NOT NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_tbl_products_image
        FOREIGN KEY (image_id) REFERENCES tbl_images (id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 6. tbl_carts
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_carts (
    id            INT             NOT NULL AUTO_INCREMENT,
    user_id       INT             NOT NULL,
    is_active     TINYINT(1)      NOT NULL DEFAULT 1,
    created_at    DATETIME        NOT NULL,
    updated_at    DATETIME        NULL,
    PRIMARY KEY (id),
    INDEX idx_tbl_carts_user_id (user_id),
    CONSTRAINT fk_tbl_carts_user
        FOREIGN KEY (user_id) REFERENCES tbl_users (id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 7. tbl_cart_items
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_cart_items (
    id           INT             NOT NULL AUTO_INCREMENT,
    cart_id      INT             NOT NULL,
    product_id   INT             NOT NULL,
    quantity     INT             NOT NULL DEFAULT 1,
    created_at   DATETIME        NOT NULL,
    updated_at   DATETIME        NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_tbl_cart_items_cart_product (cart_id, product_id),
    INDEX idx_tbl_cart_items_cart_id (cart_id),
    INDEX idx_tbl_cart_items_product_id (product_id),
    CONSTRAINT fk_tbl_cart_items_cart
        FOREIGN KEY (cart_id) REFERENCES tbl_carts (id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_cart_items_product
        FOREIGN KEY (product_id) REFERENCES tbl_products (id)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 8. tbl_orders (full column set; VARCHARs match tblOrdersMap + checkout DTOs)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_orders (
    id                                  INT             NOT NULL AUTO_INCREMENT,
    user_id                             INT             NOT NULL,
    order_number                        VARCHAR(50)     NOT NULL,
    customer_name                       VARCHAR(150)    NOT NULL,
    contact_number                      VARCHAR(50)     NOT NULL,
    delivery_address                    VARCHAR(100)    NOT NULL,
    payment_method                      VARCHAR(100)    NOT NULL,
    payment_type                        VARCHAR(50)     NULL,
    total_amount                        DECIMAL(12,2)   NOT NULL,
    remaining_balance                   DECIMAL(12,2)   NOT NULL DEFAULT 0,
    order_status                        VARCHAR(50)     NOT NULL DEFAULT 'Pending',
    delivery_type                       VARCHAR(50)     NULL DEFAULT 'COURIER',
    meetup_datetime                     DATETIME        NULL COMMENT 'Scheduled meetup (from payment or reschedule)',
    rejection_reason                    VARCHAR(500)    NULL,
    payment_deadline                    DATETIME        NULL,
    payment_deadline_reminder_sent_for  DATETIME        NULL,
    payment_deadline_reminder_sent_at   DATETIME        NULL,
    cancellation_reason                 VARCHAR(500)    NULL,
    inventory_released                  TINYINT(1)      NOT NULL DEFAULT 0,
    is_layaway                          TINYINT(1)      NOT NULL DEFAULT 0,
    created_at                          DATETIME        NOT NULL,
    last_status_update_at               DATETIME        NULL,
    is_archived                         TINYINT(1)      NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    INDEX idx_tbl_orders_user_id (user_id),
    INDEX idx_tbl_orders_created_at (created_at),
    CONSTRAINT fk_tbl_orders_user
        FOREIGN KEY (user_id) REFERENCES tbl_users (id)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 9. tbl_payments (tblPaymentsMap: reference_number 100, payment_channel 50)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_payments (
    id                    INT             NOT NULL AUTO_INCREMENT,
    order_id              INT             NOT NULL,
    amount                DECIMAL(12,2)   NOT NULL,
    payment_status        VARCHAR(50)     NOT NULL DEFAULT 'SUBMITTED',
    rejection_reason      VARCHAR(500)    NULL,
    proof_image_id        INT             NULL,
    reference_number      VARCHAR(100)    NULL,
    meetup_datetime       DATETIME        NULL,
    payment_channel       VARCHAR(50)     NULL,
    verified_by_user_id   INT             NULL,
    created_at            DATETIME        NOT NULL,
    verified_at           DATETIME        NULL,
    allow_resubmit        TINYINT(1)      NOT NULL DEFAULT 1 COMMENT '1=buyer may resubmit after reject, 0=final reject',
    PRIMARY KEY (id),
    INDEX idx_tbl_payments_order_id (order_id),
    CONSTRAINT fk_tbl_payments_order
        FOREIGN KEY (order_id) REFERENCES tbl_orders (id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_payments_proof_image
        FOREIGN KEY (proof_image_id) REFERENCES tbl_images (id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_payments_verified_by
        FOREIGN KEY (verified_by_user_id) REFERENCES tbl_users (id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 10. tbl_deliveries (courier 100, tracking_number 200, tracking_link 500)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_deliveries (
    id                  INT             NOT NULL AUTO_INCREMENT,
    order_id            INT             NOT NULL,
    delivery_type       VARCHAR(50)     NOT NULL,
    courier             VARCHAR(100)    NULL,
    dispatch_date       DATETIME        NULL,
    eta                 DATETIME        NULL,
    tracking_number     VARCHAR(200)    NULL,
    tracking_link       VARCHAR(500)    NULL,
    meetup_datetime     DATETIME        NULL,
    delivery_status     VARCHAR(50)     NOT NULL DEFAULT 'PENDING',
    created_at          DATETIME        NOT NULL,
    updated_at          DATETIME        NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_tbl_deliveries_order_id (order_id),
    CONSTRAINT fk_tbl_deliveries_order
        FOREIGN KEY (order_id) REFERENCES tbl_orders (id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 11. tbl_meetup_outcomes
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
        FOREIGN KEY (order_id) REFERENCES tbl_orders (id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_meetup_outcomes_receipt
        FOREIGN KEY (receipt_image_id) REFERENCES tbl_images (id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 12. tbl_order_items (product_name snapshot max 200, image_url 500 per map)
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
        FOREIGN KEY (order_id) REFERENCES tbl_orders (id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_order_items_product
        FOREIGN KEY (product_id) REFERENCES tbl_products (id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 13. tbl_payment_settings
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_payment_settings (
    id                  INT             NOT NULL AUTO_INCREMENT,
    payment_type        VARCHAR(50)     NOT NULL,
    account_name        VARCHAR(150)    NULL,
    account_number      VARCHAR(100)    NULL,
    qr_image_id         INT             NULL,
    is_active           TINYINT(1)      NOT NULL DEFAULT 1,
    created_at          DATETIME        NOT NULL,
    updated_at          DATETIME        NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_payment_settings_type_active (payment_type, is_active),
    CONSTRAINT fk_payment_settings_qr_image
        FOREIGN KEY (qr_image_id) REFERENCES tbl_images (id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 14. tbl_meetup_settings
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
-- 15. tbl_layaway_settings (includes resubmit_window_days, grace_period_days)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_layaway_settings (
    id                      INT             NOT NULL AUTO_INCREMENT,
    downpayment_percentage  DECIMAL(5,2)    NOT NULL DEFAULT 30,
    months_allowed          INT             NOT NULL DEFAULT 3,
    resubmit_window_days    INT             NOT NULL DEFAULT 3,
    grace_period_days       INT             NOT NULL DEFAULT 0,
    created_at              DATETIME        NOT NULL,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 16. tbl_activity_logs
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
        FOREIGN KEY (user_id) REFERENCES tbl_users (id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- Seeds (same behavior as CreateTables_And_Seed.sql)
-- ============================================================================

INSERT INTO tbl_roles (name, description, created_at) VALUES
('ADMIN',   'Technical authority',  NOW()),
('MANAGER', 'Business authority',   NOW()),
('STAFF',   'Operational support',  NOW()),
('BUYER',   'Customer role',        NOW())
ON DUPLICATE KEY UPDATE name = VALUES(name);

INSERT INTO tbl_users (role_id, email, username, first_name, last_name, username_changed_at, password_hash, password_salt, is_verified, is_active, created_at)
SELECT r.id, 'systemadmin@solteira.local', 'SystemAdmin', 'System', 'Admin', NULL, 'SEEDME', 'SEEDME', 1, 1, NOW() FROM tbl_roles r WHERE r.name = 'ADMIN' LIMIT 1
ON DUPLICATE KEY UPDATE email = VALUES(email);

INSERT INTO tbl_users (role_id, email, username, first_name, last_name, username_changed_at, password_hash, password_salt, is_verified, is_active, created_at)
SELECT r.id, 'manager@solteira.local', 'Manager', 'Manager', 'Account', NULL, 'SEEDME', 'SEEDME', 1, 1, NOW() FROM tbl_roles r WHERE r.name = 'MANAGER' LIMIT 1
ON DUPLICATE KEY UPDATE email = VALUES(email);

INSERT INTO tbl_users (role_id, email, username, first_name, last_name, username_changed_at, password_hash, password_salt, is_verified, is_active, created_at)
SELECT r.id, 'staff@solteira.local', 'Staff', 'Staff', 'Account', NULL, 'SEEDME', 'SEEDME', 1, 1, NOW() FROM tbl_roles r WHERE r.name = 'STAFF' LIMIT 1
ON DUPLICATE KEY UPDATE email = VALUES(email);

INSERT INTO tbl_users (role_id, email, username, first_name, last_name, username_changed_at, password_hash, password_salt, is_verified, is_active, created_at)
SELECT r.id, 'buyer@solteira.local', 'Buyer', 'Buyer', 'Account', NULL, 'SEEDME', 'SEEDME', 1, 1, NOW() FROM tbl_roles r WHERE r.name = 'BUYER' LIMIT 1
ON DUPLICATE KEY UPDATE email = VALUES(email);

INSERT INTO tbl_payment_settings (payment_type, account_name, account_number, qr_image_id, is_active, created_at, updated_at)
SELECT 'GCASH', 'GCash', '', NULL, 1, NOW(), NOW() FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM tbl_payment_settings WHERE payment_type = 'GCASH' AND is_active = 1);

INSERT INTO tbl_payment_settings (payment_type, account_name, account_number, qr_image_id, is_active, created_at, updated_at)
SELECT 'BANK_TRANSFER', 'Bank Transfer', '', NULL, 1, NOW(), NOW() FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM tbl_payment_settings WHERE payment_type = 'BANK_TRANSFER' AND is_active = 1);

-- Optional: align legacy rows where payment proof implies cash meetup (safe no-op on empty DB)
UPDATE tbl_orders o
INNER JOIN (
    SELECT order_id, MAX(meetup_datetime) AS meetup_datetime
    FROM tbl_payments
    WHERE meetup_datetime IS NOT NULL
    GROUP BY order_id
) p ON p.order_id = o.id
SET o.delivery_type = 'MEETUP',
    o.order_status = 'Ready for Meetup',
    o.meetup_datetime = COALESCE(o.meetup_datetime, p.meetup_datetime)
WHERE o.order_status = 'Paid';

-- ============================================================================
-- New database: open https://localhost:YOUR_PORT/Account/SeedTrialPasswords
-- ============================================================================
