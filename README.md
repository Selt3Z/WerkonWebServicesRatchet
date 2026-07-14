# WWS Ratchet

CRM for a small auto service workshop. Built as a practical MVP for real daily use and as a full stack portfolio project.

## Current features

### Authentication and roles
- Cookie based login (`POST /api/auth/login`, `GET /api/auth/me`)
- Roles: **Administrator**, **Manager**, **Mechanic**
- Seed admin on first API start (see `Seed__*` variables in `.env.example`)
- User management (admin only): list, create, edit, deactivate

| Area | Administrator | Manager | Mechanic |
|------|---------------|---------|----------|
| Clients, vehicles, visits, reminders | yes | yes | yes |
| Service catalog read | yes | yes | yes |
| Service catalog edit | yes | yes | no |
| Assign mechanic on visit | yes | yes | no |
| Users, settings, organization profile, journal | yes | no | no |
| Delete visit service items | yes | yes | yes |

### Domain
- **Archive**: soft archive for clients, vehicles, visits (`PATCH .../archive`, `PATCH .../restore`); hard delete only for empty records (admin)
- **Clients**: CRUD, search by name and phone, details with vehicles
- **Vehicles**: CRUD per client, search by plate and VIN, details with owner and visits
- **Visits**: CRUD, status (including close as Completed), mechanic assignment, service items, PDF work order
- **Reminders**: create, view, close; linked to visit or vehicle
- **Service catalog**: CRUD for admin and manager; read only for mechanic
- **Settings**: timezone, light/dark theme
- **Organization profile**: legal name, address, contacts, logo (used in PDF)
- **Journal (audit log)**: admin view of entity changes
- **Infinite scroll**: all DB-backed lists (clients, catalog, users, journal) load in chunks (`skip`/`take`) and fetch the next chunk on scroll

### Frontend (Blazor)
- Today (`/`) and schedule (`/schedule`) with visits and reminders by day
- Clients, vehicles, visits (list, details, create, edit)
- Reminders (create, details)
- Service catalog (full edit for admin/manager, read only for mechanic)
- Visit details: close visit, download PDF, assign mechanic, manage service items
- Settings and organization profile
- Users (admin)
- Journal (admin)
- Localization: Russian, English, Japanese

### PDF
- Work order PDF per visit (`GET /api/visits/{id}/work-order`)
- QuestPDF, generated on the server
- Organization logo and requisites from settings

## Tech stack
- ASP.NET Core Web API
- Blazor Web App (interactive server)
- PostgreSQL
- Entity Framework Core + migrations (applied automatically on API start)
- ASP.NET Core Identity
- QuestPDF
- Docker Compose (full stack: PostgreSQL, API, Web, Caddy, backups)

## Project structure
- `WerkonWebServicesRatchet` — backend API
- `WerkonWebServicesRatchet.Web` — Blazor frontend
- `WerkonWebServicesRatchet.Tests` — xUnit tests
- `docs/RELEASE_READINESS.md` — подробная документация по подготовке к релизу (архив, Docker, бэкапы, тесты)

## Running locally

### 1. Start PostgreSQL

From the `WerkonWebServicesRatchet` folder (where `compose.yaml` lives):

```bash
docker compose up -d
```

### 2. Run both projects

In Visual Studio set both as startup projects:
- `WerkonWebServicesRatchet`
- `WerkonWebServicesRatchet.Web`

Or run API and Web separately from each project folder.

On first API start:
- EF Core migrations are applied automatically (`IdentitySeedHostedService`)
- Default roles are created
- Admin user is seeded if missing (credentials from environment variables or `appsettings.Development.json`)

Manual migration (optional, usually not needed):

```powershell
Update-Database
```

### 3. Open the app

Blazor UI calls the API through `RatchetApiClient`. Use the URL shown in the Web project launch profile (typically `https://localhost:7xxx`).

## Docker deployment (self-hosted)

Full stack from the repository root:

```bash
cp .env.example .env
# edit .env: POSTGRES_PASSWORD, ConnectionStrings__DefaultConnection, Seed__AdminPassword

docker compose up -d --build
```

**First time only** (PowerShell as Administrator, from repo root):

```powershell
powershell -ExecutionPolicy Bypass -File docker\setup-hosts.ps1
```

Then open **`http://ratchet.local`** in the browser.

### Services

| Service | Role |
|---------|------|
| `postgres` | PostgreSQL 17 |
| `api` | ASP.NET Core API (migrations + seed on start) |
| `web` | Blazor Server UI |
| `proxy` | Caddy reverse proxy (ports 80/443) |
| `backup` | Scheduled `pg_dump` to `./backups` |

### Health checks

| Endpoint | Service | Purpose |
|----------|---------|---------|
| `GET /health` | API | Liveness (process is up) |
| `GET /health/ready` | API | Readiness (PostgreSQL reachable) |
| `GET /health` | Web | Liveness |

Docker Compose uses `/health/ready` for the API `depends_on` chain.

### Production on the local network (LAN)

The app is intended to run **inside the workshop LAN**, not on the public internet.

1. Install Docker on a PC or small server that stays on (e.g. `192.168.1.50`).
2. `docker compose up -d --build` on that machine.
3. Keep in `.env`:

```
CADDY_ADDRESS=ratchet.local
CADDY_GLOBAL_OPTIONS=auto_https off
```

4. On **each** office PC, map the name to the **server IP** (not `127.0.0.1`):
   - hosts file: `192.168.1.50    ratchet.local`, or
   - router / Windows DNS: `ratchet.local` → server IP.

5. Open `http://ratchet.local` from any PC on the LAN.

HTTPS and Let's Encrypt are not required for a closed LAN. The firewall on the server should block ports 80/443 from the internet if the machine is reachable from outside.

### Backups and ransomware protection

The `backup` service writes compressed dumps to `./backups/daily` (7 days) and `./backups/weekly` (4 weeks).

**Encrypted copy on a second local disk (restic):**

1. Create a folder on the second drive, for example `D:\RatchetBackups\restic-repo`.
2. In `.env`:

```
RESTIC_HOST_PATH=D:/RatchetBackups/restic-repo
RESTIC_REPOSITORY=/restic-repo
RESTIC_PASSWORD=strong-restic-password
```

Docker mounts `RESTIC_HOST_PATH` into the backup container at `/restic-repo`. Restic encrypts every snapshot with `RESTIC_PASSWORD`. Daily SQL dumps on the project disk (`./backups/daily`) remain separate from the encrypted repository.

Optional **remote** restic (S3, etc.) instead of a local path:

```
RESTIC_HOST_PATH=
RESTIC_REPOSITORY=s3:s3.amazonaws.com/my-bucket/ratchet-backups
RESTIC_PASSWORD=strong-restic-password
```

Rule of thumb (3-2-1): keep local dumps, a restic copy on separate storage, and periodically verify restore:

```bash
docker compose exec backup sh /usr/local/bin/restore-test.sh
```

Store `RESTIC_PASSWORD` separately from the server when possible. For S3, use Object Lock / immutability so a compromised host cannot delete old snapshots.

### Tests

```bash
dotnet test WerkonWebServicesRatchet.Tests/WerkonWebServicesRatchet.Tests.csproj
```

## Configuration

Secrets are **not** stored in `appsettings.json`. Use environment variables (see `.env.example`):

```
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=ratchet_db;Username=postgres;Password=...
Seed__AdminUserName=admin
Seed__AdminPassword=...
Seed__AdminDisplayName=Administrator
```

For local IDE runs, defaults live in `WerkonWebServicesRatchet/appsettings.Development.json`.

## Main API endpoints

### Auth
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`

### Users (admin)
- `GET /api/users?skip=&take=`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`

List endpoints below accept `skip` and `take` query parameters and return `{ "items": [...], "hasMore": true }`.

### Clients
- `GET /api/clients?name=&phone=&skip=&take=`
- `GET /api/clients/{id}`
- `GET /api/clients/{id}/details`
- `POST /api/clients`
- `PUT /api/clients/{id}`
- `PATCH /api/clients/{id}/archive`
- `PATCH /api/clients/{id}/restore`
- `DELETE /api/clients/{id}` (admin, empty client only)

### Vehicles
- `GET /api/vehicles`
- `GET /api/vehicles/{id}`
- `GET /api/vehicles/{id}/details`
- `GET /api/clients/{clientId}/vehicles`
- `POST /api/clients/{clientId}/vehicles`
- `PUT /api/vehicles/{id}`
- `PATCH /api/vehicles/{id}/archive`
- `PATCH /api/vehicles/{id}/restore`
- `DELETE /api/vehicles/{id}` (admin, no visits/reminders)

### Visits
- `GET /api/visits/by-day?date=`
- `GET /api/visits/{id}`
- `GET /api/visits/{id}/details`
- `GET /api/visits/{id}/work-order`
- `GET /api/vehicles/{vehicleId}/visits`
- `POST /api/vehicles/{vehicleId}/visits`
- `PUT /api/visits/{id}`
- `PATCH /api/visits/{id}/status`
- `PATCH /api/visits/{id}/mechanic`
- `PATCH /api/visits/{id}/archive`
- `PATCH /api/visits/{id}/restore`
- `DELETE /api/visits/{id}` (admin, no service items)
- `GET /api/visits/mechanics`

### Visit service items
- `GET /api/visits/{visitId}/service-items`
- `POST /api/visits/{visitId}/service-items`
- `DELETE /api/visits/{visitId}/service-items/{itemId}`

### Reminders
- `GET /api/reminders/by-day?date=`
- `GET /api/reminders/{id}`
- `POST /api/reminders`
- `PATCH /api/reminders/{id}/close`

### Service catalog
- `GET /api/catalog-services?search=&activeOnly=&skip=&take=`
- `GET /api/catalog-services/{id}`
- `POST /api/catalog-services`
- `PUT /api/catalog-services/{id}`
- `DELETE /api/catalog-services/{id}`

### Settings
- `GET /api/settings`
- `PUT /api/settings`
- `PUT /api/settings/timezone`
- `GET /api/settings/organization`
- `PUT /api/settings/organization`
- `PUT /api/settings/organization/logo`
- `DELETE /api/settings/organization/logo`

### Journal (admin)
- `GET /api/audit-log?skip=&take=`

### Health
- `GET /health` (liveness)
- `GET /health/ready` (readiness, includes database)

## Notes

MVP oriented project. Focus: clear domain structure, workshop friendly workflow, role based access, offline capable PDF generation.
