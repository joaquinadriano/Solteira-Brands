# Solteira & Brands — Data Dictionary (Corrected)

**Aligned with:** `system_analysis_reference.txt`, `ER_Diagram_Updated.md`, `Class_Diagram_Updated.md`.

**Sibling artifacts:** [ER_Diagram_Updated.md](./ER_Diagram_Updated.md) · [Class_Diagram_Updated.md](./Class_Diagram_Updated.md) · **[Four-column table layout (Attribute / Description / Data Type / Example)](./Data_Dictionary_Four_Column_Format.md)**  
**Conventions:** SQL column names in **snake_case**; C#/EF property names in **PascalCase** (shown in “Entity property” column). Types reflect MySQL + EF usage; booleans stored as `TINYINT(1)`.

---

## Reference: controlled vocabularies

### `tbl_orders.order_status` (string)

Values from `Solteira_Brands.Constants.OrderStatus` (exact string stored):

| Value |
|-------|
| Pending |
| Pending Approval |
| Pending Payment |
| Pending Downpayment |
| Approved |
| Awaiting Payment Verification |
| Awaiting Meetup |
| Layaway Active |
| Paid |
| In Delivery |
| Ready for Meetup |
| No Show |
| Meetup Rescheduled |
| Completed |
| Rejected |
| Cancelled |

**Note:** `Awaiting Meetup` is defined in code but not assigned by the current controller flow (meetup uses `Ready for Meetup`). `Pending Payment` / `Pending Downpayment` are used in lifecycle/deadline checks; payment rejection keeps `Awaiting Payment Verification` in the main reject path.

### `tbl_payments.payment_status` (string, typically UPPER)

From `PaymentStatus` constants:

| Value | Typical use |
|-------|-------------|
| SUBMITTED | Default when buyer submits proof (`SubmitPayment`) |
| VERIFIED | After admin approval |
| REJECTED | After admin rejection |
| PENDING | Defined; not default on create |
| AWAITING_VERIFICATION | Defined for transitions |
| EXPIRED | Terminal (rules allow transition) |

### `tbl_deliveries.delivery_status` (string, UPPER)

| PENDING | IN_TRANSIT | DELIVERED |

### `tbl_orders.payment_type`

| FULL | LAYAWAY | CASH_MEETUP |

### `tbl_orders.delivery_type`

| COURIER | MEETUP |

### `tbl_payment_settings.payment_type`

| GCASH | BANK_TRANSFER |

### `tbl_verification_tokens.token_type`

| EmailVerification | PasswordReset |

### `tbl_meetup_outcomes.status` (observed in app)

| READY_FOR_MEETUP | COMPLETED | NO_SHOW | CANCELLED |

### `tbl_roles.name` (seeded)

| ADMIN | MANAGER | STAFF | BUYER |

---

## tbl_roles

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| name | Name | VARCHAR(50) | NO | UK | Role code |
| description | Description | VARCHAR(255) | YES | | Human description |
| created_at | CreatedAt | DATETIME | NO | | Created timestamp |

---

## tbl_users

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| role_id | RoleId | INT | NO | FK → tbl_roles.id | Assigned role |
| email | Email | VARCHAR(150) | NO | UK | Login / verification |
| first_name | FirstName | VARCHAR(50) | NO | | Buyer profile |
| last_name | LastName | VARCHAR(50) | NO | | Buyer profile |
| username | Username | VARCHAR(150) | NO | UK | Forms auth identity |
| password_hash | PasswordHash | VARCHAR(255) | NO | | PBKDF2 hash (Base64) |
| password_salt | PasswordSalt | VARCHAR(255) | NO | | Salt (Base64) |
| is_verified | IsVerified | TINYINT(1) | NO | | Email OTP completed |
| is_active | IsActive | TINYINT(1) | NO | | Soft disable |
| must_change_password | MustChangePassword | TINYINT(1) | NO | | Staff forced reset |
| created_at | CreatedAt | DATETIME | NO | | Registration time |
| username_changed_at | UsernameChangedAt | DATETIME | YES | | 30-day rename cooldown |
| accepted_terms | AcceptedTerms | TINYINT(1) | NO | | Terms acceptance |
| accepted_at | AcceptedAt | DATETIME | YES | | When terms accepted |
| avatar_image_id | AvatarImageId | INT | YES | FK → tbl_images.id | Profile image |

**Database-only (not mapped in EF):** `auth_provider` VARCHAR(50) NOT NULL DEFAULT `'LOCAL'` — present in `CreateTables_And_Seed.sql` migrations; application model does not map it.

---

## tbl_verification_tokens

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| user_id | UserId | INT | NO | FK → tbl_users.id | Owner |
| token_value | TokenValue | VARCHAR(255) | NO | | Hashed OTP / token |
| token_type | TokenType | VARCHAR(50) | NO | | EmailVerification, PasswordReset |
| expires_at | ExpiresAt | DATETIME | NO | | Expiry |
| is_used | IsUsed | TINYINT(1) | NO | | Single-use flag |
| created_at | CreatedAt | DATETIME | NO | | Created |

---

## tbl_images

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| image_path | ImagePath | VARCHAR(255) | NO | | URL prefix |
| image_file_name | ImageFileName | VARCHAR(255) | NO | | File name on disk |
| created_at | CreatedAt | DATETIME | NO | | Inserted |
| updated_at | UpdatedAt | DATETIME | NO | | Last update |

---

## tbl_products

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| name | Name | VARCHAR(30) | NO | | Catalog name |
| category | Category | VARCHAR(100) | NO | | **Denormalized** label (no category table) |
| description | Description | VARCHAR(255) | YES | | Short description |
| price | Price | DECIMAL(10,2) | NO | | Unit price |
| material | Material | VARCHAR(50) | YES | | Spec |
| stones | Stones | VARCHAR(50) | YES | | Spec |
| weight | Weight | VARCHAR(50) | YES | | Spec |
| length | Length | VARCHAR(50) | YES | | Spec |
| stock_quantity | StockQuantity | INT | NO | | Available qty |
| initial_stock | InitialStock | INT | YES | | Original qty at create |
| image_id | ImageId | INT | YES | FK → tbl_images.id | Product photo |
| is_best_seller | IsBestSeller | TINYINT(1) | NO | | Flag |
| is_featured | IsFeatured | TINYINT(1) | NO | | Flag |
| is_archived | IsArchived | TINYINT(1) | NO | | Soft hide |
| created_at | CreatedAt | DATETIME | NO | | Created |
| updated_at | UpdatedAt | DATETIME | NO | | Updated |

---

## tbl_carts

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| user_id | UserId | INT | NO | FK → tbl_users.id | Owner |
| is_active | IsActive | TINYINT(1) | NO | | Active cart flag |
| created_at | CreatedAt | DATETIME | NO | | Created |
| updated_at | UpdatedAt | DATETIME | YES | | Last update |

---

## tbl_cart_items

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| cart_id | CartId | INT | NO | FK → tbl_carts.id | Parent cart |
| product_id | ProductId | INT | NO | FK → tbl_products.id | Line product |
| quantity | Quantity | INT | NO | | Units |
| created_at | CreatedAt | DATETIME | NO | | Created |
| updated_at | UpdatedAt | DATETIME | YES | | Last update |

**Constraint (SQL):** UNIQUE (`cart_id`, `product_id`).

---

## tbl_orders

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| user_id | UserId | INT | NO | FK → tbl_users.id | Buyer |
| order_number | OrderNumber | VARCHAR(50) | NO | | e.g. ORD-{id} |
| customer_name | CustomerName | VARCHAR(150) | NO | | Checkout contact |
| contact_number | ContactNumber | VARCHAR(50) | NO | | Phone |
| delivery_address | DeliveryAddress | VARCHAR(100) | NO | | Address (max 100) |
| payment_method | PaymentMethod | VARCHAR(100) | NO | | Display method label |
| payment_type | PaymentType | VARCHAR(50) | YES | | FULL, LAYAWAY, CASH_MEETUP |
| total_amount | TotalAmount | DECIMAL(12,2) | NO | | Order total |
| remaining_balance | RemainingBalance | DECIMAL(12,2) | NO | | After verified payments |
| order_status | OrderStatus | VARCHAR(50) | NO | | See controlled vocab |
| payment_deadline | PaymentDeadline | DATETIME | YES | | **UTC** enforcement in `OrderDeadlineService` |
| payment_deadline_reminder_sent_for | PaymentDeadlineReminderSentFor | DATETIME | YES | | Dedup reminders |
| payment_deadline_reminder_sent_at | PaymentDeadlineReminderSentAt | DATETIME | YES | | When reminder sent |
| cancellation_reason | CancellationReason | VARCHAR(500) | YES | | Cancel / auto-cancel context |
| inventory_released | InventoryReleased | TINYINT(1) | NO | | Stock restore guard |
| is_layaway | IsLayaway | TINYINT(1) | NO | | Convenience flag |
| delivery_type | DeliveryType | VARCHAR(50) | YES | | COURIER, MEETUP |
| meetup_datetime | MeetupDatetime | DATETIME | YES | | Scheduled meetup |
| rejection_reason | RejectionReason | VARCHAR(500) | YES | | Order reject reason |
| created_at | CreatedAt | DATETIME | NO | | Placed |
| last_status_update_at | LastStatusUpdateAt | DATETIME | YES | | Status change |
| is_archived | IsArchived | TINYINT(1) | NO | | Hide from admin ops |

---

## tbl_order_items

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| order_id | OrderId | INT | NO | FK → tbl_orders.id | Parent order |
| product_id | ProductId | INT | YES | FK → tbl_products.id | Optional link (snapshot) |
| product_name | ProductName | VARCHAR(200) | NO | | Denormalized name |
| quantity | Quantity | INT | NO | | Units |
| unit_price | UnitPrice | DECIMAL(10,2) | NO | | Snapshot price |
| image_url | ImageUrl | VARCHAR(500) | YES | | Snapshot image URL |

---

## tbl_payments

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| order_id | OrderId | INT | NO | FK → tbl_orders.id | Parent order |
| amount | Amount | DECIMAL(12,2) | NO | | Submitted amount |
| payment_status | PaymentStatus | VARCHAR(50) | NO | | SUBMITTED / VERIFIED / … |
| rejection_reason | RejectionReason | VARCHAR(500) | YES | | If rejected |
| proof_image_id | ProofImageId | INT | YES | FK → tbl_images.id | Proof file |
| reference_number | ReferenceNumber | VARCHAR(100) | YES | | GCash / bank ref |
| meetup_datetime | MeetupDatetime | DATETIME | YES | | Meetup scheduling |
| payment_channel | PaymentChannel | VARCHAR(50) | YES | | GCash / Bank / Meetup label |
| verified_by_user_id | VerifiedByUserId | INT | YES | FK → tbl_users.id | Staff verifier |
| created_at | CreatedAt | DATETIME | NO | | Submitted |
| verified_at | VerifiedAt | DATETIME | YES | | Verified/rejected time |
| allow_resubmit | AllowResubmit | INT | NO | | **0** = final reject; **1** = buyer may resubmit |

---

## tbl_deliveries

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| order_id | OrderId | INT | NO | FK → tbl_orders.id | Parent order |
| delivery_type | DeliveryType | VARCHAR(50) | NO | | Shipment / meetup row type |
| courier | Courier | VARCHAR(100) | YES | | Carrier name |
| dispatch_date | DispatchDate | DATETIME | YES | | Dispatched |
| eta | Eta | DATETIME | YES | | ETA |
| tracking_number | TrackingNumber | VARCHAR(200) | YES | | Tracking # |
| tracking_link | TrackingLink | VARCHAR(500) | YES | | Tracking URL |
| meetup_datetime | MeetupDatetime | DATETIME | YES | | Meetup (if used) |
| delivery_status | DeliveryStatus | VARCHAR(50) | NO | | PENDING / IN_TRANSIT / DELIVERED |
| created_at | CreatedAt | DATETIME | NO | | Created |
| updated_at | UpdatedAt | DATETIME | NO | | Updated |

---

## tbl_meetup_outcomes

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| order_id | OrderId | INT | NO | FK → tbl_orders.id | Related order (**1:M** history) |
| status | Status | VARCHAR(50) | NO | | READY_FOR_MEETUP, COMPLETED, NO_SHOW, CANCELLED |
| receipt_image_id | ReceiptImageId | INT | YES | FK → tbl_images.id | Meetup proof |
| created_at | CreatedAt | DATETIME | NO | | Record time |

---

## tbl_payment_settings

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| payment_type | PaymentType | VARCHAR(50) | NO | | GCASH, BANK_TRANSFER |
| account_name | AccountName | VARCHAR(150) | YES | | Display name |
| account_number | AccountNumber | VARCHAR(100) | YES | | Account # |
| qr_image_id | QrImageId | INT | YES | FK → tbl_images.id | QR image |
| is_active | IsActive | TINYINT(1) | NO | | Active row |
| created_at | CreatedAt | DATETIME | NO | | Created |
| updated_at | UpdatedAt | DATETIME | NO | | Updated |

---

## tbl_meetup_settings

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| location_name | LocationName | VARCHAR(150) | NO | | Label |
| address | Address | VARCHAR(500) | NO | | Address text |
| is_active | IsActive | TINYINT(1) | NO | | Active location |
| created_at | CreatedAt | DATETIME | NO | | Created |

---

## tbl_layaway_settings

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| downpayment_percentage | DownpaymentPercentage | DECIMAL(5,2) | NO | | % down |
| months_allowed | MonthsAllowed | INT | NO | | Installment months |
| resubmit_window_days | ResubmitWindowDays | INT | YES | | Config (SQL default 3) |
| grace_period_days | GracePeriodDays | INT | YES | | Config (SQL default 0) |
| created_at | CreatedAt | DATETIME | NO | | Created |

---

## tbl_activity_logs

| SQL column | Entity property | Data type | Null | Key | Description |
|------------|-----------------|-----------|------|-----|-------------|
| id | Id | INT | NO | PK | Surrogate key |
| user_id | UserId | INT | NO | FK → tbl_users.id | Actor |
| username | Username | VARCHAR(150) | NO | | Denormalized |
| role_name | RoleName | VARCHAR(50) | NO | | Denormalized |
| action | Action | VARCHAR(100) | NO | | Action label |
| module | Module | VARCHAR(50) | NO | | Area |
| entity_id | EntityId | INT | YES | | Related id |
| details | Details | VARCHAR(500) | YES | | Extra text |
| created_at | CreatedAt | DATETIME | NO | | Log time |

---

## Cross-artifact consistency checklist

- **ERD entities** ↔ **dictionary tables** ↔ **class diagram** entity names (`tbl_*` / `*Model`) use the same fields.
- **No category table** in ERD or dictionary.
- **payment_deadline** documented as UTC for enforcement; UI may format in local time via helpers.
- **allow_resubmit** documented as integer semantics (0/1).
