CREATE TABLE IF NOT EXISTS `tbl_policy_settings` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `policy_type` VARCHAR(50) NOT NULL,
  `content` LONGTEXT NOT NULL,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  INDEX `idx_tbl_policy_settings_type_active` (`policy_type`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
