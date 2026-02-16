-- ============================================================================
-- Solteira & Brands - Create tbl_roles, tbl_users + seed roles + 1 test account
-- Database: SolteiraBrands (MySQL / MariaDB)
-- Test login: admin / Test123@  (password stored as SHA256 hex)
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
-- 2. tbl_users
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_users (
    id              INT             NOT NULL AUTO_INCREMENT,
    role_id         INT             NOT NULL,
    first_name      VARCHAR(100)    NOT NULL,
    last_name       VARCHAR(100)    NOT NULL,
    email           VARCHAR(150)    NOT NULL,
    password_hash   VARCHAR(255)    NOT NULL,
    is_verified     TINYINT(1)      NOT NULL DEFAULT 0,
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    created_at      DATETIME        NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_tbl_users_email (email),
    CONSTRAINT fk_tbl_users_role
        FOREIGN KEY (role_id)
        REFERENCES tbl_roles (id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 3. Seed roles
-- ----------------------------------------------------------------------------
INSERT INTO tbl_roles (name, description, created_at) VALUES
('ADMIN',   'Technical authority',    NOW()),
('MANAGER', 'Business authority',    NOW()),
('STAFF',   'Operational support',   NOW()),
('BUYER',   'Customer role',         NOW())
ON DUPLICATE KEY UPDATE name = name;

-- ----------------------------------------------------------------------------
-- 4. Test account (password = Test123@, stored as SHA256 hex)
-- ----------------------------------------------------------------------------
INSERT INTO tbl_users (role_id, first_name, last_name, email, password_hash, is_verified, is_active, created_at)
SELECT r.id, 'Admin', 'Test', 'admin', 'fae4ee5710378f2aa14e4cf2f6597794cc59263b8546e550e35ec0a845239108', 1, 1, NOW()
FROM tbl_roles r WHERE r.name = 'ADMIN' LIMIT 1
ON DUPLICATE KEY UPDATE first_name = first_name;

-- Test login: email = admin, password = Test123@
