# BioLicense Portal - Implementation Plan (Internal Ticketing System)

## Context
Portal ini mengotomasi proses generate license BLE-Tracking dan multi-aplikasi lainnya melalui sistem **Ticketing Internal**. 

**Peran Pengguna**:
- **Owner**: Kontrol penuh (Aplikasi, User Management, Approval).
- **TechnicalEngineer**: Bertugas me-review dan men-generate license (Approval).
- **Distributor (Internal Team)**: Bertugas mengajukan request license (Ticketing) untuk client dan mengirimkannya.

---

## Arsitektur Project
- **Domain**: Entities, Enums.
- **Application**: IService, Service Implementation, DTOs.
- **Infrastructure**: DbContext, Migrations, Repositories (Class-based).
- **WebAPI**: Minimal API Endpoints.

---

## Database Schema (Core Tables)

### `users`
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| username | nvarchar(255) UNIQUE | |
| password | nvarchar(255) | BCrypt hash |
| role | nvarchar(50) | Owner / TechnicalEngineer / Distributor |

### `refresh_tokens`
Untuk manajemen login session.

### `applications` & `application_features`
Manajemen produk dan fitur (seperti BLE-Tracking master data, tracking, dll).

### `license_requests` (Ticketing)
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| application_id | FK applications | |
| requester_user_id | FK users | Distributor (Internal) |
| customer_name | nvarchar(255) | |
| customer_email | nvarchar(255) | |
| machine_id | nvarchar(255) | |
| license_type | nvarchar(50) | |
| license_tier | nvarchar(50) | |
| features | nvarchar(max) | |
| status | nvarchar(50) | Pending / Approved / Rejected / Completed |
| approver_user_id | FK users | Owner / Engineer |
| license_record_id | FK licenses | Nullable |
| notes | nvarchar(max) | |
| created_at, updated_at | datetime2 | |

### `licenses`
Record hasil generate license yang sudah di-sign.

### `audit_logs`
Tracking aktivitas user.

---

## API Endpoints (Revised)

### Auth `/api/v1/auth`
- `POST /login`: Login → JWT.
- `POST /refresh`: Refresh token.
- `POST /seed-owner`: Inisialisasi Owner pertama.

### Applications `/api/v1/applications` (Owner only)
- CRUD Aplikasi & Feature.
- `POST /{id}/generate-keypair`: Generate private/public key.

### License Requests `/api/v1/license-requests`
- `GET /`: List requests (Owner/Engineer=All, Distributor=Own).
- `POST /`: Buat request baru (Distributor).
- `GET /{id}`: Detail request.
- `POST /{id}/approve`: Approve & Generate License (Owner/Engineer).
- `POST /{id}/reject`: Tolak request (Owner/Engineer).

### Licenses `/api/v1/licenses`
- `GET /`: List history license.
- `GET /{id}/download`: Download file `.lic`.

---

## Key Flows (Ticketing)

### Flow 1: Requesting (Distributor)
1. Distributor mengisi form request: Pilih App, Nama Client, Machine ID, Fitur.
2. Status tersimpan sebagai `Pending`.

### Flow 2: Approval & Generation (Engineer/Owner)
1. Engineer melihat daftar `Pending` requests.
2. Engineer klik `Approve`.
3. Sistem otomatis men-generate file `.lic` menggunakan Private Key aplikasi.
4. Status request berubah menjadi `Completed`.
5. Link download license muncul di detail tiket.

---

## Implementation Phases

### Phase 1: Foundation (COMPLETED)
- [x] Project Structure (Domain, Application, Infrastructure).
- [x] Entities & Enums.
- [x] DbContext & Repositories.
- [x] Initial Migrations.

### Phase 2: Authentication
- JWT Auth, Password Hashing, Role-Based Access.

### Phase 3: Application Management
- CRUD App, Feature Management, KeyPair Generation logic.

### Phase 4: Ticketing Module
- License Request CRUD, Approval Logic, State Transition.

### Phase 5: Licensing Engine
- Integration with `Standard.Licensing` for signing.

### Phase 6: Email & Polish
- Auto-email notification, Dashboard stats, Audit logs.
