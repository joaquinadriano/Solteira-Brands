# Solteira & Brands — ER Diagram (Updated)

**Source:** `system_analysis_reference.txt` (current schema: EF6 + MySQL `CreateTables_And_Seed.sql`).  
**Consistency:** Table and column names match `Data_Dictionary_Corrected.md` and entity attributes in `Class_Diagram_Updated.md`.

**Sibling artifacts:** [Data_Dictionary_Corrected.md](./Data_Dictionary_Corrected.md) · [Class_Diagram_Updated.md](./Class_Diagram_Updated.md)

**Notes**

- There is **no** `tbl_categories` entity; product category is the string column `tbl_products.category`.
- `tbl_users.auth_provider` exists in the database script but is **not** mapped in EF; it appears only in the data dictionary footnote.
- `tbl_meetup_outcomes` is **1:M** per order (history of outcomes), not 1:1.

## Diagram (Mermaid)

```mermaid
erDiagram
    tbl_roles {
        int id PK
        string name UK
        string description
        datetime created_at
    }

    tbl_users {
        int id PK
        int role_id FK
        string email UK
        string first_name
        string last_name
        string username UK
        string password_hash
        string password_salt
        bool is_verified
        bool is_active
        bool must_change_password
        datetime created_at
        datetime username_changed_at
        bool accepted_terms
        datetime accepted_at
        int avatar_image_id FK
    }

    tbl_verification_tokens {
        int id PK
        int user_id FK
        string token_value
        string token_type
        datetime expires_at
        bool is_used
        datetime created_at
    }

    tbl_images {
        int id PK
        string image_path
        string image_file_name
        datetime created_at
        datetime updated_at
    }

    tbl_products {
        int id PK
        string name
        string category
        string description
        decimal price
        string material
        string stones
        string weight
        string length
        int stock_quantity
        int initial_stock
        int image_id FK
        bool is_best_seller
        bool is_featured
        bool is_archived
        datetime created_at
        datetime updated_at
    }

    tbl_carts {
        int id PK
        int user_id FK
        bool is_active
        datetime created_at
        datetime updated_at
    }

    tbl_cart_items {
        int id PK
        int cart_id FK
        int product_id FK
        int quantity
        datetime created_at
        datetime updated_at
    }

    tbl_orders {
        int id PK
        int user_id FK
        string order_number
        string customer_name
        string contact_number
        string delivery_address
        string payment_method
        string payment_type
        decimal total_amount
        decimal remaining_balance
        string order_status
        datetime payment_deadline
        datetime payment_deadline_reminder_sent_for
        datetime payment_deadline_reminder_sent_at
        string cancellation_reason
        bool inventory_released
        bool is_layaway
        string delivery_type
        datetime meetup_datetime
        string rejection_reason
        datetime created_at
        datetime last_status_update_at
        bool is_archived
    }

    tbl_order_items {
        int id PK
        int order_id FK
        int product_id FK
        string product_name
        int quantity
        decimal unit_price
        string image_url
    }

    tbl_payments {
        int id PK
        int order_id FK
        decimal amount
        string payment_status
        string rejection_reason
        int proof_image_id FK
        string reference_number
        datetime meetup_datetime
        string payment_channel
        int verified_by_user_id FK
        datetime created_at
        datetime verified_at
        int allow_resubmit
    }

    tbl_deliveries {
        int id PK
        int order_id FK
        string delivery_type
        string courier
        datetime dispatch_date
        datetime eta
        string tracking_number
        string tracking_link
        datetime meetup_datetime
        string delivery_status
        datetime created_at
        datetime updated_at
    }

    tbl_meetup_outcomes {
        int id PK
        int order_id FK
        string status
        int receipt_image_id FK
        datetime created_at
    }

    tbl_payment_settings {
        int id PK
        string payment_type
        string account_name
        string account_number
        int qr_image_id FK
        bool is_active
        datetime created_at
        datetime updated_at
    }

    tbl_meetup_settings {
        int id PK
        string location_name
        string address
        bool is_active
        datetime created_at
    }

    tbl_layaway_settings {
        int id PK
        decimal downpayment_percentage
        int months_allowed
        int resubmit_window_days
        int grace_period_days
        datetime created_at
    }

    tbl_activity_logs {
        int id PK
        int user_id FK
        string username
        string role_name
        string action
        string module
        int entity_id
        string details
        datetime created_at
    }

    tbl_roles ||--o{ tbl_users : assigns
    tbl_users ||--o{ tbl_verification_tokens : has
    tbl_images ||--o{ tbl_users : "avatar_image_id (optional)"
    tbl_users ||--o{ tbl_orders : places
    tbl_users ||--o{ tbl_carts : owns
    tbl_users ||--o{ tbl_activity_logs : generates

    tbl_images ||--o{ tbl_products : product_image
    tbl_images ||--o{ tbl_payment_settings : qr_code
    tbl_images ||--o{ tbl_payments : payment_proof
    tbl_images ||--o{ tbl_meetup_outcomes : receipt
    tbl_users ||--o{ tbl_payments : verifies

    tbl_carts ||--o{ tbl_cart_items : contains
    tbl_products ||--o{ tbl_cart_items : referenced_by

    tbl_orders ||--o{ tbl_order_items : line_items
    tbl_products ||--o{ tbl_order_items : optional_link

    tbl_orders ||--o{ tbl_payments : has
    tbl_orders ||--o{ tbl_deliveries : has
    tbl_orders ||--o{ tbl_meetup_outcomes : has
```

## Relationship summary (cardinality)

| Parent | Child | Cardinality | FK column(s) |
|--------|--------|-------------|--------------|
| tbl_roles | tbl_users | 1 : M | user.role_id |
| tbl_users | tbl_verification_tokens | 1 : M | user_id |
| tbl_users | tbl_orders | 1 : M | user_id |
| tbl_users | tbl_carts | 1 : M | user_id |
| tbl_users | tbl_activity_logs | 1 : M | user_id |
| tbl_users | tbl_payments (as verifier) | 1 : M | verified_by_user_id (optional) |
| tbl_images | tbl_users (avatar) | 1 : M | avatar_image_id (optional) |
| tbl_images | tbl_products | 1 : M | image_id (optional) |
| tbl_images | tbl_payment_settings | 1 : M | qr_image_id (optional) |
| tbl_images | tbl_payments | 1 : M | proof_image_id (optional) |
| tbl_images | tbl_meetup_outcomes | 1 : M | receipt_image_id (optional) |
| tbl_carts | tbl_cart_items | 1 : M | cart_id |
| tbl_products | tbl_cart_items | 1 : M | product_id |
| tbl_orders | tbl_order_items | 1 : M | order_id |
| tbl_products | tbl_order_items | 1 : M (optional) | product_id (nullable) |
| tbl_orders | tbl_payments | 1 : M | order_id |
| tbl_orders | tbl_deliveries | 1 : M | order_id |
| tbl_orders | tbl_meetup_outcomes | 1 : M | order_id |

`tbl_meetup_settings` and `tbl_layaway_settings` are **standalone configuration** tables (no FKs to orders).
