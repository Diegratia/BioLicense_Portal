# BioLicense Portal - Implementation Plan

## Context
Saat ini proses generate license dilakukan secara manual melalui console app `LicenseGenerator`. Portal ini mengotomasi seluruh proses melalui **sistem ticketing internal** dan mendukung multi-aplikasi.

**Tujuan**: API-only portal untuk manajemen license multi-aplikasi dengan workflow ticketing.

**Peran Pengguna**:
- **Owner**: Kontrol penuh (Aplikasi, User Management, Approval license)
- **TechnicalEngineer**: Review dan generate license (Approval)
- **Distributor**: Mengajukan request license (Ticketing) untuk client

---

## Arsitektur Project (4-Layer Clean Architecture)

```
BioLicense_Portal/
├── BioLicense_Portal.sln
├── docs/
├── src/
│   ├── Domain/                        # Class Library - Pure domain
│   │   ├── Entities/                  # BaseEntity, User, Application, ApplicationFeature,
│   │   │                              # LicenseRecord, LicenseRequest, RefreshToken, AuditLog
│   │   └── Enums/                     # UserRole, LicenseType, LicenseTier, LicenseStatus,
│   │                                  # ApplicationType, LicenseRequestStatus
│   ├── Application/                   # Class Library - Business contracts
│   │   ├── Interfaces/                # IAuthService, IApplicationService, ILicenseService,
│   │   │                              # IJwtTokenService, IPasswordHasher, IKeyGeneratorService
│   │   ├── Common/Constants/          # Messages.cs (static response messages)
│   │   ├── Exceptions/                # NotFoundException, BusinessException, etc.
│   │   ├── Extensions/                # ApiResponseHelper.cs
│   │   └── Mappings/                  # MappingProfile.cs (AutoMapper)
│   ├── Infrastructure/                # Class Library - Data & implementation
│   │   ├── Data/
│   │   │   ├── BioLicenseDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/              # UserRepository, ApplicationRepository, LicenseRepository
│   │   ├── Services/                  # AuthService, ApplicationService, LicenseService
│   │   └── Security/                  # JwtTokenService, PasswordHasher, KeyGeneratorService
│   └── WebAPI/                        # ASP.NET Core Web API (entry point)
│       ├── Program.cs
│       ├── Controllers/               # AuthController, ApplicationController, LicenseController
│       └── Middleware/                # CustomExceptionMiddleware
```

**Project References**: WebAPI → Domain + Application + Infrastructure, Infrastructure → Domain + Application, Application → Domain, Domain → none

---

## Database Schema

### `users`
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| username | nvarchar(255) UNIQUE | Login username |
| password_hash | nvarchar(255) | BCrypt hash |
| email | nvarchar(255) UNIQUE | |
| full_name | nvarchar(255) | |
| role | nvarchar(50) | Owner / TechnicalEngineer / Distributor |
| status | int (default 1) | 1=active, 0=inactive |
| last_login_at | datetime2 nullable | |
| created_at, updated_at | datetime2 | Auto by DbContext |
| created_by, updated_by | uniqueidentifier nullable | |

### `refresh_tokens`
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| user_id | uniqueidentifier FK users | |
| token | nvarchar(512) | Random bytes |
| expiry_date | datetime2 | |

### `applications`
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| name | nvarchar(255) UNIQUE | e.g. "BLE-Tracking" |
| slug | nvarchar(100) UNIQUE | URL-safe identifier |
| application_type | nvarchar(50) | PeopleTracking/VMS/Parking/SMR/Signage |
| description | nvarchar(max) | |
| private_key_encrypted | nvarchar(max) | AES-encrypted private key |
| public_key | nvarchar(max) | Public key string |
| key_passphrase | nvarchar(255) | AES-encrypted passphrase |
| tier_configs | nvarchar(max) | JSON: tier → {maxBeacons, maxReaders, defaultFeatures} |
| status | int (default 1) | |
| created_at, updated_at | datetime2 | |
| created_by, updated_by | uniqueidentifier nullable | |

### `application_features`
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

### `license_requests` (Ticketing)
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| application_id | uniqueidentifier FK applications | |
| requester_user_id | uniqueidentifier FK users | Distributor |
| customer_name | nvarchar(255) | |
| customer_email | nvarchar(255) | |
| machine_id | nvarchar(255) | Target machine ID |
| license_type | nvarchar(50) | Trial/Annual/Perpetual |
| license_tier | nvarchar(50) | Core/Professional/Enterprise/Custom |
| license_parameters | nvarchar(max) | JSON: {maxBeacons, maxReaders, ...} |
| features | nvarchar(max) | Comma-separated feature keys |
| expiry_date | datetime2 nullable | Custom expiry override |
| request_status | nvarchar(50) | Pending/Approved/Rejected/Completed |
| rejection_reason | nvarchar(max) nullable | Alasan penolakan |
| approver_user_id | uniqueidentifier FK users nullable | Engineer/Owner |
| license_record_id | uniqueidentifier FK licenses nullable | Link ke license setelah generate |
| notes | nvarchar(max) | Catatan tambahan |
| requested_at, processed_at | datetime2 nullable | |
| created_at, updated_at | datetime2 | |

### `licenses` (Generated License Records)
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| license_id | uniqueidentifier UNIQUE | GUID dalam .lic file |
| application_id | uniqueidentifier FK applications | |
| customer_name | nvarchar(255) | |
| customer_email | nvarchar(255) | |
| machine_id | nvarchar(255) | |
| license_type | nvarchar(50) | Trial/Annual/Perpetual |
| license_tier | nvarchar(50) | Core/Professional/Enterprise/Custom |
| license_parameters | nvarchar(max) | JSON |
| features | nvarchar(max) | Comma-separated |
| custom_attributes | nvarchar(max) | JSON atribut tambahan |
| license_content | nvarchar(max) | Signed .lic content |
| issued_at | datetime2 | |
| expired_at | datetime2 | |
| status | nvarchar(50) default 'Active' | Active/Revoked/Expired |
| revoked_at | datetime2 nullable | |
| revoked_reason | nvarchar(max) nullable | |
| generated_by_user_id | uniqueidentifier FK users | |
| assigned_to_user_id | uniqueidentifier FK users nullable | Distributor penerima |
| created_at, updated_at | datetime2 | |

### `audit_logs`
| Column | Type | Notes |
|--------|------|-------|
| id | uniqueidentifier PK | |
| event_name | nvarchar(100) | e.g. "License.Generated" |
| entity_name | nvarchar(255) | |
| entity_id | uniqueidentifier nullable | |
| actor_user_id | uniqueidentifier FK users | |
| actor_username | nvarchar(255) | Denormalized |
| details | nvarchar(max) | JSON |
| ip_address | nvarchar(50) nullable | |
| event_time | datetime2 | |

---

## API Endpoints

### Auth `/api/auth`
| Method | Path | Auth | Desc | Status |
|--------|------|------|------|--------|
| POST | /login | None | Login → JWT + refresh token | Done |
| POST | /seed-owner | None | Seed akun Owner pertama | Done |
| POST | /seed-distributor | None | Seed akun Distributor | Done |
| POST | /refresh | None | Refresh access token | **TODO** |
| POST | /logout | Bearer | Invalidate refresh token | **TODO** |
| POST | /change-password | Bearer | Ubah password | **TODO** |

### Applications `/api/application` (Owner only)
| Method | Path | Desc | Status |
|--------|------|------|--------|
| GET | / | List aplikasi (search/filter) | Done |
| GET | /{id} | Detail aplikasi | Done |
| POST | / | Register aplikasi baru (+ auto keypair) | Done |
| PUT | /{id} | Update aplikasi | Done |
| DELETE | /{id} | Soft-delete aplikasi | Done |
| POST | /{id}/features | Tambah feature | Done |
| PUT | /features/{featureId} | Update feature | Done |
| DELETE | /features/{featureId} | Hapus feature | Done |
| POST | /{id}/generate-keypair | Regenerate key pair | **TODO** |
| GET | /{id}/public-key | Ambil public key | **TODO** |

### License Requests `/api/licenses/requests` (Bearer)
| Method | Path | Auth | Desc | Status |
|--------|------|------|------|--------|
| POST | / | Distributor | Buat request baru | Done |
| GET | /my | Distributor | List request sendiri | Done |
| GET | /pending | Engineer/Owner | List pending requests | Done |
| POST | /{id}/approve | Engineer/Owner | Approve & generate .lic | **TODO** (approve tanpa generate) |
| POST | /{id}/reject | Engineer/Owner | Tolak request | Done |

### Licenses `/api/licenses` (Bearer)
| Method | Path | Auth | Desc | Status |
|--------|------|------|------|--------|
| GET | / | Bearer | List semua license | **TODO** |
| GET | /{id} | Bearer | Detail license | **TODO** |
| GET | /{id}/download | Bearer | Download file .lic | **TODO** |
| POST | /{id}/revoke | Owner | Revoke license | **TODO** |

### Dashboard `/api/dashboard` (Owner only)
| Method | Path | Desc | Status |
|--------|------|------|--------|
| GET | /stats | Statistik keseluruhan | **TODO** |
| GET | /application/{appId}/stats | Statistik per app | **TODO** |

---

## Key Flows

### Flow 1: Distributor Request License (Ticketing)
1. Distributor kirim `POST /api/licenses/requests` dengan applicationId, customerName, machineId, licenseType, licenseTier, features
2. System validasi app exists, resolve TierConfig (auto-fill parameters untuk non-Custom tier)
3. Status disimpan sebagai `Pending`
4. **Current code**: `LicenseService.CreateRequestAsync()`

### Flow 2: Engineer Approve & Generate License
1. Engineer lihat `GET /api/licenses/requests/pending`
2. Engineer klik approve `POST /api/licenses/requests/{id}/approve`
3. System load private key dari `applications` table → decrypt passphrase
4. Build attributes dictionary (MachineID, LicenseType, LicenseTier, MaxBeacons, MaxReaders, Features)
5. Sign license menggunakan **Standard.Licensing**:
   ```csharp
   var license = License.New()
       .WithUniqueIdentifier(Guid.NewGuid())
       .As(LicenseType.Standard)
       .ExpiresAt(expiration)
       .WithAdditionalAttributes(attributes)
       .LicensedTo(customerName, customerEmail)
       .CreateAndSignWithPrivateKey(privateKey, passphrase);
   ```
6. Create `LicenseRecord` di database
7. Link `LicenseRequest.LicenseRecordId` → license record
8. Set `LicenseRequest.RequestStatus = "Completed"`
9. Kirim email ke distributor/customer dengan .lic attachment
10. Tulis audit log
11. **Current gap**: step 4-10 belum diimplementasi

### Flow 3: Distributor Download License
1. Distributor akses `GET /api/licenses/{id}/download`
2. Return file `.lic` (dari `license_content` column)
3. **Current gap**: endpoint belum ada

---

## NuGet Packages

### Domain
- (no external dependencies)

### Application
- AutoMapper 16.1.1

### Infrastructure
- Microsoft.EntityFrameworkCore.SqlServer 9.0
- BCrypt.Net-Next 4.1.0
- System.IdentityModel.Tokens.Jwt 8.0

### WebAPI
- Microsoft.AspNetCore.OpenApi 9.0.15
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0
- Microsoft.EntityFrameworkCore.SqlServer 9.0
- Microsoft.EntityFrameworkCore.Design 9.0
- AutoMapper 16.1.1
- AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1
- DotNetEnv 3.2.0

### Still needed
- **Standard.Licensing** (Infrastructure) - untuk generate .lic file
- **MailKit** (Infrastructure) - untuk email delivery

---

## Configuration (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=bio_license_portal;..."
  },
  "Jwt": {
    "Key": "...",
    "Issuer": "biolicense-portal",
    "Audience": "biolicense-portal-users"
  }
}
```

---

## Implementation Progress

### Phase 1: Foundation — DONE
- [x] Project structure (4-layer clean architecture)
- [x] Domain entities & enums
- [x] Application interfaces & DTOs
- [x] Infrastructure DbContext + 3 migrations
- [x] Repositories (User, Application, License)
- [x] API response wrapper + custom exceptions + message constants

### Phase 2: Authentication — 80%
- [x] JwtTokenService (HMAC-SHA256, refresh token)
- [x] PasswordHasher (BCrypt)
- [x] AuthService (login, refresh token service, seed owner/distributor)
- [x] AuthController (login, seed-owner, seed-distributor)
- [x] JWT Bearer middleware
- [ ] **POST /auth/refresh** (service ada, controller endpoint belum)
- [ ] **POST /auth/logout**
- [ ] **POST /auth/change-password**

### Phase 3: Application Management — 90%
- [x] ApplicationService (CRUD + auto keypair generation + TierConfigs)
- [x] FeatureService (add/update/delete per application)
- [x] ApplicationController (full CRUD + features)
- [x] KeyGeneratorService (RSA-2048 + AES encryption)
- [ ] **POST /{id}/generate-keypair** (regenerate)
- [ ] **GET /{id}/public-key**

### Phase 4: Ticketing Module — 70%
- [x] LicenseService.CreateRequestAsync (Distributor buat request)
- [x] LicenseService.GetMyRequestsAsync (list own requests)
- [x] LicenseService.GetPendingRequestsAsync (list pending)
- [x] LicenseService.RejectRequestAsync (reject + reason)
- [x] LicenseController (create, my, pending, reject endpoints)
- [ ] **ApproveRequest harus generate .lic file** (P0 - critical)

### Phase 5: Licensing Engine — NOT STARTED
- [ ] Install Standard.Licensing NuGet
- [ ] Implement LicenseGeneratorService (wrap Standard.Licensing signing)
- [ ] Integrate ke ApproveRequest: generate .lic → create LicenseRecord → link ke request
- [ ] **GET /api/licenses** (list all licenses)
- [ ] **GET /api/licenses/{id}** (detail)
- [ ] **GET /api/licenses/{id}/download** (download .lic file)
- [ ] **POST /api/licenses/{id}/revoke** (revoke)

### Phase 6: Email Delivery — NOT STARTED
- [ ] EmailService (SMTP via MailKit)
- [ ] Kirim .lic sebagai email attachment saat license di-generate
- [ ] Email notifikasi ke distributor saat request di-approve/reject

### Phase 7: Dashboard & Audit — NOT STARTED
- [ ] AuditService (write to audit_logs table)
- [ ] AuditMiddleware (automatic request logging)
- [ ] Dashboard endpoints (stats, per-app stats)

---

## Referensi File Existing (BLE-Tracking)
- `Services.API/LicenseGenerator/Program.cs` → Standard.Licensing API (key gen, license signing)
- `Shared/BusinessLogic.Services/Implementation/LicenseService.cs` → License validation pattern
- `Shared/Shared.Contracts/Enum.cs` → LicenseType/LicenseTier enum values
- `Shared/BusinessLogic.Services/Feature/FeatureDefinition.cs` → Feature definitions

## Verification
- Build: `dotnet build`
- Run: `dotnet run --project src/WebAPI`
- Test flow: seed owner → login → create application → add features → create distributor → submit request → approve (generate .lic) → download .lic → verify .lic content
- Test .lic compatibility: license harus bisa di-validate oleh LicenseChecker/LicenseService BLE-Tracking yang sudah ada
