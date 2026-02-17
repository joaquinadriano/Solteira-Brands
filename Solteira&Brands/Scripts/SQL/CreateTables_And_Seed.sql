-- ============================================================================
-- Solteira & Brands — SINGLE SQL SCRIPT (update this file as schema changes)
-- Database: SolteiraBrands (MySQL / MariaDB)
-- Run in HeidiSQL. Idempotent: safe to run multiple times.
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

-- ============================================================================
-- Next step: Run the app and open in browser:
--   https://localhost:YOUR_PORT/Account/SeedTrialPasswords
-- ============================================================================
