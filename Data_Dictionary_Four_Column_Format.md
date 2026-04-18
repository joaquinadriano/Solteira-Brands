# Solteira & Brands — Data Dictionary (Four-Column Table Format)

**Purpose:** One table per database entity with columns **Attribute**, **Description**, **Data Type**, **Example** (same structure as your sample).  
**Scope:** All tables from the current schema (`system_analysis_reference.txt`, `CreateTables_And_Seed.sql`, EF maps).  
**Naming:** **Attribute** uses the actual **MySQL `snake_case`** column name.

**Conventions for “Data Type”**

- `INT(11)` = typical MySQL display width for `INT`.
- `AUTO_INCREMENT` only on integer primary keys where the script defines it.
- `FOREIGN KEY` rows include `REFERENCES parent_table(column)`.
- Booleans are stored as `TINYINT(1)` (0/1).
- **Example** for integer PKs is numeric (your sample mixed a string order code with an INT PK; here `order_number` carries values like `ORD-…`).

**Also see:** [Data_Dictionary_Corrected.md](./Data_Dictionary_Corrected.md) (full key/null/EF cross-reference).

---

## tbl_roles

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the role | INT(11), AUTO_INCREMENT, PRIMARY KEY | 2 |
| name | Role code used by the application and authorization | VARCHAR(50), NOT NULL, UNIQUE | MANAGER |
| description | Human-readable role description | VARCHAR(255), NULL | Business authority |
| created_at | When the role row was created | DATETIME, NOT NULL | 2026-04-01 09:00:00 |

---

## tbl_users

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the user account | INT(11), AUTO_INCREMENT, PRIMARY KEY | 48 |
| role_id | Role assigned to this user | INT(11), NOT NULL, FOREIGN KEY → tbl_roles(id) | 4 |
| email | Email address; used for login verification and contact | VARCHAR(150), NOT NULL, UNIQUE | buyer@example.com |
| first_name | User’s given name | VARCHAR(50), NOT NULL | Ana |
| last_name | User’s family name | VARCHAR(50), NOT NULL | Reyes |
| username | Short name used for Forms Authentication identity | VARCHAR(150), NOT NULL, UNIQUE | ana_r |
| password_hash | PBKDF2 password hash (Base64) | VARCHAR(255), NOT NULL | WGh0c2…(truncated) |
| password_salt | Salt for password hash (Base64) | VARCHAR(255), NOT NULL | c2FsdC…(truncated) |
| is_verified | 1 if email OTP verification completed | TINYINT(1), NOT NULL | 1 |
| is_active | 1 if account may sign in | TINYINT(1), NOT NULL | 1 |
| must_change_password | 1 forces staff to change password on next login | TINYINT(1), NOT NULL | 0 |
| created_at | When the account was created | DATETIME, NOT NULL | 2026-03-15 14:22:10 |
| username_changed_at | Last username change (UTC); supports 30-day cooldown | DATETIME, NULL | 2026-02-01 08:00:00 |
| accepted_terms | 1 if user accepted terms and privacy policy | TINYINT(1), NOT NULL | 1 |
| accepted_at | When terms were accepted | DATETIME, NULL | 2026-03-15 14:22:10 |
| avatar_image_id | Optional profile image; references tbl_images | INT(11), NULL, FOREIGN KEY → tbl_images(id) | 901 |
| auth_provider | Auth source (DB column; not mapped in EF) | VARCHAR(50), NOT NULL, DEFAULT 'LOCAL' | LOCAL |

---

## tbl_verification_tokens

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the token row | INT(11), AUTO_INCREMENT, PRIMARY KEY | 1001 |
| user_id | User this token belongs to | INT(11), NOT NULL, FOREIGN KEY → tbl_users(id) | 48 |
| token_value | Hashed OTP or reset token (not plaintext) | VARCHAR(255), NOT NULL | 7gK…(Base64 hash) |
| token_type | Kind of token | VARCHAR(50), NOT NULL | EmailVerification |
| expires_at | When the token is no longer valid | DATETIME, NOT NULL | 2026-04-05 12:00:30 |
| is_used | 1 if token has already been consumed | TINYINT(1), NOT NULL | 0 |
| created_at | When the token was issued | DATETIME, NOT NULL | 2026-04-05 11:59:45 |

---

## tbl_images

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the stored image metadata | INT(11), AUTO_INCREMENT, PRIMARY KEY | 901 |
| image_path | URL path prefix for the file | VARCHAR(255), NOT NULL | /Content/images_uploaded/paymentproof/ |
| image_file_name | File name on disk | VARCHAR(255), NOT NULL | a1b2c3d4-e5f6-7890.png |
| created_at | When the image row was inserted | DATETIME, NOT NULL | 2026-04-04 16:10:00 |
| updated_at | Last update to the image row | DATETIME, NOT NULL | 2026-04-04 16:10:00 |

---

## tbl_products

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the product | INT(11), AUTO_INCREMENT, PRIMARY KEY | 15 |
| name | Product display name (short) | VARCHAR(30), NOT NULL | Gold Hoop Earrings |
| category | Category label (denormalized; no category table) | VARCHAR(100), NOT NULL | Earrings |
| description | Short description | VARCHAR(255), NULL | 14k gold, polished |
| price | Unit price | DECIMAL(10,2), NOT NULL | 12500.00 |
| material | Material specification | VARCHAR(50), NULL | 14k yellow gold |
| stones | Stone specification | VARCHAR(50), NULL | None |
| weight | Weight specification | VARCHAR(50), NULL | 3.2g |
| length | Length specification | VARCHAR(50), NULL | 20mm |
| stock_quantity | Units available for sale | INT(11), NOT NULL | 12 |
| initial_stock | Original quantity when product was created | INT(11), NULL | 20 |
| image_id | Optional catalog image | INT(11), NULL, FOREIGN KEY → tbl_images(id) | 880 |
| is_best_seller | 1 if flagged as best seller | TINYINT(1), NOT NULL | 1 |
| is_featured | 1 if featured on storefront | TINYINT(1), NOT NULL | 0 |
| is_archived | 1 if hidden from active catalog | TINYINT(1), NOT NULL | 0 |
| created_at | When the product was created | DATETIME, NOT NULL | 2026-01-10 10:00:00 |
| updated_at | Last product update | DATETIME, NOT NULL | 2026-04-02 11:30:00 |

---

## tbl_carts

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the cart | INT(11), AUTO_INCREMENT, PRIMARY KEY | 33 |
| user_id | Owner of the cart | INT(11), NOT NULL, FOREIGN KEY → tbl_users(id) | 48 |
| is_active | 1 if this is the user’s active cart | TINYINT(1), NOT NULL | 1 |
| created_at | When the cart was created | DATETIME, NOT NULL | 2026-04-03 09:15:00 |
| updated_at | Last cart update | DATETIME, NULL | 2026-04-05 18:40:00 |

---

## tbl_cart_items

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the cart line | INT(11), AUTO_INCREMENT, PRIMARY KEY | 500 |
| cart_id | Parent cart | INT(11), NOT NULL, FOREIGN KEY → tbl_carts(id) | 33 |
| product_id | Product in the cart | INT(11), NOT NULL, FOREIGN KEY → tbl_products(id) | 15 |
| quantity | Number of units | INT(11), NOT NULL | 2 |
| created_at | When the line was added | DATETIME, NOT NULL | 2026-04-03 09:16:00 |
| updated_at | Last quantity or line update | DATETIME, NULL | 2026-04-03 09:20:00 |

**Constraint (SQL):** UNIQUE (`cart_id`, `product_id`).

---

## tbl_orders

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the order | INT(11), AUTO_INCREMENT, PRIMARY KEY | 127 |
| user_id | Buyer who placed the order | INT(11), NOT NULL, FOREIGN KEY → tbl_users(id) | 48 |
| order_number | Human-readable order reference | VARCHAR(50), NOT NULL | ORD-127 |
| customer_name | Contact name at checkout | VARCHAR(150), NOT NULL | Ana Reyes |
| contact_number | Contact phone at checkout | VARCHAR(50), NOT NULL | 09171234567 |
| delivery_address | Shipping or contact address (max 100 chars in app) | VARCHAR(100), NOT NULL | 123 Rizal St, Quezon City |
| payment_method | Display label for payment option chosen | VARCHAR(100), NOT NULL | Full Payment – GCash |
| payment_type | Internal type: FULL, LAYAWAY, CASH_MEETUP | VARCHAR(50), NULL | FULL |
| total_amount | Order total | DECIMAL(12,2), NOT NULL | 25000.00 |
| remaining_balance | Balance after verified payments | DECIMAL(12,2), NOT NULL | 0.00 |
| order_status | Lifecycle status (see OrderStatus constants) | VARCHAR(50), NOT NULL | Paid |
| payment_deadline | UTC deadline for payment/installment enforcement | DATETIME, NULL | 2026-04-08 23:59:59 |
| payment_deadline_reminder_sent_for | Dedup key for reminder emails | DATETIME, NULL | 2026-04-08 23:59:59 |
| payment_deadline_reminder_sent_at | When reminder was sent | DATETIME, NULL | 2026-04-07 10:00:00 |
| cancellation_reason | Reason if cancelled | VARCHAR(500), NULL | NULL |
| inventory_released | 1 if stock restoration already done | TINYINT(1), NOT NULL | 0 |
| is_layaway | Convenience flag for layaway | TINYINT(1), NOT NULL | 0 |
| delivery_type | COURIER or MEETUP | VARCHAR(50), NULL | COURIER |
| meetup_datetime | Scheduled meetup date/time | DATETIME, NULL | 2026-04-10 14:00:00 |
| rejection_reason | Reason if order rejected | VARCHAR(500), NULL | NULL |
| created_at | When the order was placed | DATETIME, NOT NULL | 2026-04-04 12:00:00 |
| last_status_update_at | Last status change timestamp | DATETIME, NULL | 2026-04-05 09:30:00 |
| is_archived | 1 hides order from operational admin lists | TINYINT(1), NOT NULL | 0 |

---

## tbl_order_items

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the order line | INT(11), AUTO_INCREMENT, PRIMARY KEY | 400 |
| order_id | Parent order | INT(11), NOT NULL, FOREIGN KEY → tbl_orders(id) | 127 |
| product_id | Optional link to product (snapshot still kept) | INT(11), NULL, FOREIGN KEY → tbl_products(id) | 15 |
| product_name | Product name at time of order | VARCHAR(200), NOT NULL | Gold Hoop Earrings |
| quantity | Units ordered | INT(11), NOT NULL | 2 |
| unit_price | Price per unit at checkout | DECIMAL(10,2), NOT NULL | 12500.00 |
| image_url | Image URL snapshot | VARCHAR(500), NULL | /Content/images_uploaded/…/x.png |

---

## tbl_payments

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the payment submission | INT(11), AUTO_INCREMENT, PRIMARY KEY | 300 |
| order_id | Order this payment applies to | INT(11), NOT NULL, FOREIGN KEY → tbl_orders(id) | 127 |
| amount | Amount submitted | DECIMAL(12,2), NOT NULL | 25000.00 |
| payment_status | SUBMITTED, VERIFIED, REJECTED, etc. | VARCHAR(50), NOT NULL | VERIFIED |
| rejection_reason | Staff reason if rejected | VARCHAR(500), NULL | NULL |
| proof_image_id | Uploaded proof image | INT(11), NULL, FOREIGN KEY → tbl_images(id) | 901 |
| reference_number | GCash/bank reference | VARCHAR(100), NULL | ABC123XYZ789 |
| meetup_datetime | Meetup slot if cash-through-meetup | DATETIME, NULL | NULL |
| payment_channel | Channel label (e.g. GCash, Bank Transfer) | VARCHAR(50), NULL | GCash |
| verified_by_user_id | Staff user who verified/rejected | INT(11), NULL, FOREIGN KEY → tbl_users(id) | 3 |
| created_at | When buyer submitted payment | DATETIME, NOT NULL | 2026-04-04 13:00:00 |
| verified_at | When staff verified or rejected | DATETIME, NULL | 2026-04-05 09:00:00 |
| allow_resubmit | 0 = final reject; 1 = buyer may resubmit | INT(11), NOT NULL | 1 |

---

## tbl_deliveries

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the delivery record | INT(11), AUTO_INCREMENT, PRIMARY KEY | 55 |
| order_id | Related order | INT(11), NOT NULL, FOREIGN KEY → tbl_orders(id) | 127 |
| delivery_type | Shipment vs meetup row semantics | VARCHAR(50), NOT NULL | COURIER |
| courier | Carrier name | VARCHAR(100), NULL | J&T Express |
| dispatch_date | When shipment was dispatched | DATETIME, NULL | 2026-04-06 08:00:00 |
| eta | Estimated arrival | DATETIME, NULL | 2026-04-08 17:00:00 |
| tracking_number | Tracking number | VARCHAR(200), NULL | JT1234567890 |
| tracking_link | Optional tracking URL | VARCHAR(500), NULL | https://example.com/track?id=JT123 |
| meetup_datetime | Meetup time if used on this row | DATETIME, NULL | NULL |
| delivery_status | PENDING, IN_TRANSIT, DELIVERED | VARCHAR(50), NOT NULL | IN_TRANSIT |
| created_at | When the delivery row was created | DATETIME, NOT NULL | 2026-04-05 10:00:00 |
| updated_at | Last update | DATETIME, NOT NULL | 2026-04-06 08:05:00 |

---

## tbl_meetup_outcomes

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the outcome row | INT(11), AUTO_INCREMENT, PRIMARY KEY | 12 |
| order_id | Order this outcome belongs to | INT(11), NOT NULL, FOREIGN KEY → tbl_orders(id) | 200 |
| status | READY_FOR_MEETUP, COMPLETED, NO_SHOW, CANCELLED | VARCHAR(50), NOT NULL | COMPLETED |
| receipt_image_id | Proof image for completed meetup | INT(11), NULL, FOREIGN KEY → tbl_images(id) | 902 |
| created_at | When this outcome was recorded | DATETIME, NOT NULL | 2026-04-10 15:30:00 |

---

## tbl_payment_settings

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the settings row | INT(11), AUTO_INCREMENT, PRIMARY KEY | 1 |
| payment_type | GCASH or BANK_TRANSFER | VARCHAR(50), NOT NULL | GCASH |
| account_name | Display account name | VARCHAR(150), NULL | Solteira GCash |
| account_number | Account or number string | VARCHAR(100), NULL | 09171234567 |
| qr_image_id | Optional QR image | INT(11), NULL, FOREIGN KEY → tbl_images(id) | 905 |
| is_active | 1 if this configuration is active | TINYINT(1), NOT NULL | 1 |
| created_at | Created timestamp | DATETIME, NOT NULL | 2026-01-01 00:00:00 |
| updated_at | Updated timestamp | DATETIME, NOT NULL | 2026-03-01 12:00:00 |

---

## tbl_meetup_settings

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the meetup location row | INT(11), AUTO_INCREMENT, PRIMARY KEY | 1 |
| location_name | Short label for the venue | VARCHAR(150), NOT NULL | Main boutique |
| address | Full address text | VARCHAR(500), NOT NULL | 456 Session Rd, Baguio City |
| is_active | 1 if this location is the active default | TINYINT(1), NOT NULL | 1 |
| created_at | When the row was created | DATETIME, NOT NULL | 2026-01-01 00:00:00 |

---

## tbl_layaway_settings

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the layaway config row | INT(11), AUTO_INCREMENT, PRIMARY KEY | 1 |
| downpayment_percentage | Required down payment percent | DECIMAL(5,2), NOT NULL | 30.00 |
| months_allowed | Number of installment months after down payment | INT(11), NOT NULL | 3 |
| resubmit_window_days | Config: resubmit window (SQL default 3) | INT(11), NULL | 3 |
| grace_period_days | Config: grace before overdue UI (SQL default 0) | INT(11), NULL | 0 |
| created_at | When the row was created | DATETIME, NOT NULL | 2026-01-01 00:00:00 |

---

## tbl_activity_logs

| Attribute | Description | Data Type | Example |
|-----------|-------------|-----------|---------|
| id | Unique identifier of the log entry | INT(11), AUTO_INCREMENT, PRIMARY KEY | 5000 |
| user_id | Staff user who performed the action | INT(11), NOT NULL, FOREIGN KEY → tbl_users(id) | 3 |
| username | Denormalized username | VARCHAR(150), NOT NULL | Manager |
| role_name | Denormalized role | VARCHAR(50), NOT NULL | MANAGER |
| action | Short action label | VARCHAR(100), NOT NULL | Order Approved |
| module | Functional area | VARCHAR(50), NOT NULL | Orders |
| entity_id | Related entity id, if any | INT(11), NULL | 127 |
| details | Extra text | VARCHAR(500), NULL | Order #ORD-127 |
| created_at | When the log was written | DATETIME, NOT NULL | 2026-04-05 09:05:00 |

---

## Coverage checklist

| Table | Section above |
|-------|----------------|
| tbl_roles | ✓ |
| tbl_users | ✓ (includes `auth_provider`) |
| tbl_verification_tokens | ✓ |
| tbl_images | ✓ |
| tbl_products | ✓ |
| tbl_carts | ✓ |
| tbl_cart_items | ✓ |
| tbl_orders | ✓ |
| tbl_order_items | ✓ |
| tbl_payments | ✓ |
| tbl_deliveries | ✓ |
| tbl_meetup_outcomes | ✓ |
| tbl_payment_settings | ✓ |
| tbl_meetup_settings | ✓ |
| tbl_layaway_settings | ✓ |
| tbl_activity_logs | ✓ |

**Total: 16 tables.**
