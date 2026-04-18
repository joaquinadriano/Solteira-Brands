-- MySQL / MariaDB (HeidiSQL-friendly; no SQL Server GO batches)
CREATE TABLE IF NOT EXISTS tbl_product_images (
    id              INT             NOT NULL AUTO_INCREMENT,
    product_id      INT             NOT NULL,
    image_id        INT             NOT NULL,
    is_primary      TINYINT(1)      NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL,
    updated_at      DATETIME        NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_tbl_product_images_product_image (product_id, image_id),
    INDEX idx_tbl_product_images_product_id (product_id),
    INDEX idx_tbl_product_images_image_id (image_id),
    CONSTRAINT fk_tbl_product_images_product
        FOREIGN KEY (product_id) REFERENCES tbl_products (id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_tbl_product_images_image
        FOREIGN KEY (image_id) REFERENCES tbl_images (id)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Backfill legacy single-image links from tbl_products.image_id.
INSERT INTO tbl_product_images (product_id, image_id, is_primary, created_at, updated_at)
SELECT p.id, p.image_id, 1, NOW(), NOW()
FROM tbl_products p
LEFT JOIN tbl_product_images pi
    ON pi.product_id = p.id
   AND pi.image_id = p.image_id
WHERE p.image_id IS NOT NULL
  AND pi.id IS NULL;

-- If a product has images but no primary, make the earliest row primary.
UPDATE tbl_product_images pi
JOIN (
    SELECT product_id, MIN(id) AS min_id
    FROM tbl_product_images
    GROUP BY product_id
) first_row ON first_row.min_id = pi.id
LEFT JOIN (
    SELECT DISTINCT product_id
    FROM tbl_product_images
    WHERE is_primary = 1
) has_primary ON has_primary.product_id = pi.product_id
SET pi.is_primary = 1,
    pi.updated_at = NOW()
WHERE has_primary.product_id IS NULL;
