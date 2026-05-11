# BioLicense Portal - Implementation Plan

## Context
Saat ini proses generate license BLE-Tracking dilakukan secara manual melalui console app `LicenseGenerator`. Owner harus menjalankan tool secara manual, copy file `.lic`, dan kirim ke customer/distributor secara manual. Portal ini akan mengotomasi seluruh proses tersebut dan mendukung multi-aplikasi (bukan hanya BLE-Tracking).

**Tujuan**: Membuat API-only portal untuk manajemen license multi-aplikasi, dimana Owner bisa generate license, mengelola distributor, dan mengirim license ke distributor.

---

## Arsitektur Project

```
BioLicense_Portal/
├── BioLicense_Portal.sln
├── docs/
│   └── plan.md                    # Dokumentasi plan
├── src/
│   ├── WebAPI/                    # ASP.NET Core Web API (entry point)
│   │   ├── Program.cs
│   │   ├── BioLicense_Portal.csproj
│   │   ├── appsettings.json
│   │   ├── Endpoints/             # Minimal API endpoint groups
│   │   │   ├── AuthEndpoints.cs
│   │   │   ├── ApplicationEndpoints.cs
│   │   │   ├── FeatureEndpoints.cs
│   │   │   ├── DistributorEndpoints.cs
│   │   │   ├── LicenseEndpoints.cs
│   │   │   └── DashboardEndpoints.cs
│   │   └── Middleware/
│   │       ├── ExceptionMiddleware.cs
│   │       └── AuditMiddleware.cs
│   ├── Core/                      # Class Library - Domain layer
│   │   ├── Entities/              # EF Core entity classes
│   │   ├── Enums/                 # UserRole, LicenseType, LicenseTier, LicenseStatus
│   │   ├── Interfaces/            # Service contracts
│   │   └── DTOs/                  # Request/Response models
│   └── Infrastructure/            # Class Library - Data & Service implementation
│       ├── Data/
│       │   ├── BioLicenseDbContext.cs
│       │   └── Migrations/
│       ├── Repositories/
│       ├── Services/
│       │   ├── AuthService.cs
│       │   ├── ApplicationService.cs
│       │   ├── LicenseGeneratorService.cs  # Wraps Standard.Licensing
│       │   ├── EmailService.cs             # SMTP email delivery
│       │   └── ...
│       └── Security/
│           ├── JwtTokenService.cs
│           └── PasswordHasher.cs
```

**Project References**: WebAPI → Core + Infrastructure, Infrastructure → Core, Core → none

---

## Database Schema

### `users` - Akun portal (Owner & Distributor)
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| username | nvarchar(255) UNIQUE | Login username |
| password | nvarchar(255) | BCrypt hash |
| email | nvarchar(255) UNIQUE | |
| full_name | nvarchar(255) | |
| role | nvarchar(50) | "Owner" / "Distributor" |
| status | int (default 1) | 1=active, 0=inactive |
| created_at, updated_at, last_login_at | datetime2 | |

### `refresh_tokens`
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| user_id | uniqueidentifier FK users | |
| token | nvarchar(512) | |
| expiry_date | datetime2 | |

### `applications` - Produk/aplikasi terdaftar
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| name | nvarchar(255) UNIQUE | e.g. "BLE-Tracking" |
| slug | nvarchar(100) UNIQUE | URL-safe identifier |
| description | nvarchar(max) | |
| private_key_encrypted | nvarchar(max) | Encrypted ECDSA private key |
| public_key | nvarchar(max) | Public key string |
| key_passphrase | nvarchar(255) | AES-encrypted passphrase |
| status | int (default 1) | |
| created_at, updated_at | datetime2 | |
| created_by | uniqueidentifier FK users | |

### `application_features` - Feature per aplikasi
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| application_id | uniqueidentifier FK applications | |
| feature_key | nvarchar(255) | e.g. "core.tracking" |
| display_name | nvarchar(255) | |
| description | nvarchar(500) | |
| category | nvarchar(50) | "core" / "module" |
| is_active | bit (default 1) | |
| created_at | datetime2 | |

UNIQUE(`application_id`, `feature_key`)

### `distributors` - Profil perusahaan distributor
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| company_name | nvarchar(255) | |
| contact_email | nvarchar(255) | |
| contact_phone | nvarchar(50) | |
| address | nvarchar(500) | |
| status | int (default 1) | |
| created_at, updated_at | datetime2 | |

### `distributor_applications` - Autorisasi & limit distributor per app
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| distributor_id | uniqueidentifier FK distributors | |
| application_id | uniqueidentifier FK applications | |
| max_trial_licenses | int (default 0) | |
| max_annual_licenses | int (default 0) | |
| max_perpetual_licenses | int (default 0) | |
| trial_created, annual_created, perpetual_created | int (default 0) | Running counters |
| can_generate | bit (default 1) | |
| assigned_at | datetime2 | |
| assigned_by | uniqueidentifier FK users | |

UNIQUE(`distributor_id`, `application_id`)

### `distributor_users` - Link user ke distributor
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| user_id | uniqueidentifier FK users UNIQUE | |
| distributor_id | uniqueidentifier FK distributors | |

### `licenses` - Record setiap license yang di-generate
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| license_guid | uniqueidentifier UNIQUE | GUID dalam .lic file |
| application_id | uniqueidentifier FK applications | |
| customer_name | nvarchar(255) | |
| customer_email | nvarchar(255) | |
| machine_id | nvarchar(255) | Target machine ID |
| license_type | nvarchar(50) | Trial/Annual/Perpetual |
| license_tier | nvarchar(50) | Core/Professional/Enterprise/Custom |
| max_beacons | int | BLE-Tracking specific |
| max_readers | int | BLE-Tracking specific |
| features | nvarchar(max) | Comma-separated feature keys |
| custom_attributes | nvarchar(max) | JSON untuk atribut tambahan |
| license_content | nvarchar(max) | Signed .lic content |
| expires_at | datetime2 | |
| status | nvarchar(50) default 'Active' | Active/Revoked/Expired |
| revoked_at, revoked_reason | | |
| generated_by_user_id | uniqueidentifier FK users | |
| assigned_to_distributor_id | uniqueidentifier FK distributors nullable | |
| created_at, updated_at | datetime2 | |

### `audit_logs`
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| event_name | nvarchar(100) | e.g. "License.Generated" |
| entity_name | nvarchar(255) | |
| entity_id | uniqueidentifier | |
| actor_user_id | uniqueidentifier FK users | |
| actor_username | nvarchar(255) | |
| details | nvarchar(max) | JSON |
| ip_address | nvarchar(50) | |
| event_time | datetime2 | |

---

## API Endpoints

### Auth `/api/v1/auth`
| Method | Path | Auth | Desc |
|--------|------|------|------|
| POST | /auth/login | None | Login → JWT + refresh token |
| POST | /auth/refresh | None | Refresh access token |
| POST | /auth/logout | Bearer | Invalidate refresh token |
| POST | /auth/change-password | Bearer | Ubah password |
| POST | /auth/seed-owner | None | Seed akun Owner pertama (sekali saja) |

### Applications `/api/v1/applications` (Owner only)
| Method | Path | Desc |
|--------|------|------|
| GET | /applications | List semua aplikasi |
| GET | /applications/{id} | Detail aplikasi |
| POST | /applications | Register aplikasi baru |
| PUT | /applications/{id} | Update info aplikasi |
| DELETE | /applications/{id} | Soft-delete |
| POST | /applications/{id}/generate-keypair | Generate ECDSA key pair |
| GET | /applications/{id}/public-key | Ambil public key |

### Features `/api/v1/applications/{appId}/features` (Owner only)
| Method | Path | Desc |
|--------|------|------|
| GET | .../features | List features |
| POST | .../features | Tambah feature |
| PUT | .../features/{id} | Update feature |
| DELETE | .../features/{id} | Hapus feature |

### Distributors `/api/v1/distributors` (Owner only)
| Method | Path | Desc |
|--------|------|------|
| GET | /distributors | List semua distributor |
| GET | /distributors/{id} | Detail distributor |
| POST | /distributors | Buat distributor (+ buat user account) |
| PUT | /distributors/{id} | Update info |
| DELETE | /distributors/{id} | Soft-delete |
| POST | /distributors/{id}/assign-application | Authorisasi app + set limits |
| PUT | /distributors/{id}/applications/{appId}/limits | Update limits |
| DELETE | /distributors/{id}/applications/{appId} | Cabut authorisasi |
| POST | /distributors/{id}/reset-usage | Reset counter license |

### Licenses `/api/v1/licenses` (Owner + Distributor)
| Method | Path | Auth | Desc |
|--------|------|------|------|
| GET | /licenses | Bearer | List (Owner=semua, Distributor=miliknya) |
| GET | /licenses/{id} | Bearer | Detail license |
| POST | /licenses/generate | Bearer | Generate license baru |
| POST | /licenses/{id}/assign-distributor | Owner | Assign ke distributor |
| POST | /licenses/{id}/revoke | Owner | Revoke license |
| GET | /licenses/{id}/download | Bearer | Download file .lic |

### Dashboard `/api/v1/dashboard` (Owner only)
| Method | Path | Desc |
|--------|------|------|
| GET | /dashboard/stats | Statistik keseluruhan |
| GET | /dashboard/application/{appId}/stats | Statistik per app |
| GET | /dashboard/distributor/{distId}/usage | Usage distributor vs limits |

---

## Key Flows

### Flow 1: Owner Generate License
1. Owner kirim `POST /licenses/generate` dengan applicationId, customerName, machineId, licenseType, licenseTier, features
2. System load private key dari `applications` table
3. Validasi feature keys ada di `application_features`
4. Hitung expiry: Trial=7 hari, Annual=1 tahun, Perpetual=100 tahun
5. Build attributes dictionary → sign dengan Standard.Licensing
6. Simpan ke `licenses` table + audit log
7. Kirim email ke customer/distributor dengan .lic file attachment
8. Return license record + download URL

### Flow 2: Owner Assign License ke Distributor
1. Owner kirim `POST /licenses/{id}/assign-distributor` dengan distributorId
2. Validasi license Active, distributor terauthorisasi untuk app tersebut
3. Set `assigned_to_distributor_id` pada license record
4. Kirim email notifikasi ke distributor
5. Audit log
6. Distributor bisa lihat & download license tsb

### Flow 3: Distributor Generate License (dengan limit)
1. Distributor kirim `POST /licenses/generate`
2. System extract distributorId dari JWT claims
3. Cek `distributor_applications`: authorized? `can_generate=true`?
4. Cek counter vs limit (misal annual_created < max_annual_licenses)
5. Jika lolos, generate license seperti Flow 1
6. Increment counter di `distributor_applications` (dalam transaction yang sama)
7. Kirim email ke customer dengan .lic file attachment

### Flow 4: Key Pair Generation
1. `Standard.Licensing.Security.Cryptography.KeyGenerator.Create()`
2. `GenerateKeyPair()` → `ToEncryptedPrivateKeyString(passphrase)` + `ToPublicKeyString()`
3. Simpan ke `applications` table (passphrase dienkripsi AES dengan master key)

---

## NuGet Packages
- **WebAPI**: Microsoft.AspNetCore.OpenApi, Swashbuckle.AspNetCore, Microsoft.AspNetCore.Authentication.JwtBearer, Standard.Licensing, MailKit
- **Infrastructure**: Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Design, Standard.Licensing, BCrypt.Net-Next, MailKit
- **Core**: (no external dependencies)

## Auth & Configuration
- JWT Bearer (BCrypt password hashing, refresh tokens)
- Role-based: "Owner" / "Distributor" policies
- SQL Server database `BioLicensePortalDb`
- **Key Storage**: Private key passphrase dienkripsi AES dengan master key dari `appsettings.json`, disimpan di DB
- **Registration**: Hanya Owner yang bisa membuat akun distributor (tidak ada self-registration)
- **License Delivery**: API download endpoint + kirim email otomatis ke distributor/customer (SMTP via appsettings)

---

## Implementation Phases

### Phase 1: Foundation
1. Buat `Core` class library (entities, enums, interfaces, DTOs)
2. Buat `Infrastructure` class library (DbContext, configurations, repositories)
3. Initial EF Core migration

### Phase 2: Authentication
4. JwtTokenService, PasswordHasher, CurrentUserService
5. AuthService (login, refresh, change-password, seed-owner)
6. AuthEndpoints + JWT middleware

### Phase 3: Application & Feature Management
7. ApplicationService + FeatureService
8. ApplicationEndpoints + FeatureEndpoints (Owner only)

### Phase 4: Distributor Management
9. DistributorService (CRUD + assign app + limits)
10. DistributorEndpoints (Owner only)

### Phase 5: License Generation (Core Feature)
11. LicenseGeneratorService (wraps Standard.Licensing)
12. Limit enforcement untuk distributor
13. LicenseEndpoints (generate, list, download, revoke, assign)

### Phase 6: Email Delivery
14. EmailService (SMTP wrapper - kirim .lic file sebagai attachment)
15. Integrate ke license generation: setelah generate → email ke distributor/customer

### Phase 7: Dashboard & Audit
16. AuditService
17. DashboardEndpoints
18. ExceptionMiddleware + AuditMiddleware

---

## Referensi File Existing
- `BLE-Tracking/.../LicenseGenerator/Program.cs` → Pola Standard.Licensing (key gen line 34-51, license creation line 53-141)
- `BLE-Tracking/.../LicenseService.cs` → Pola validasi license (activation line 65-151)
- `BLE-Tracking/.../Enum.cs` → LicenseType/LicenseTier enum values (line 40-54)
- `BLE-Tracking/.../FeatureDefinition.cs` → Feature definitions (line 10-127)

## Verification
- Build: `dotnet build`
- Run: `dotnet run --project src/WebAPI`
- Test flow: seed owner → login → create application → generate keypair → add features → create distributor → assign app to distributor → generate license → download .lic → verify .lic content
- Test .lic compatibility: license yang di-generate harus bisa di-validate oleh LicenseChecker/LicenseService BLE-Tracking yang sudah ada
