# Werkon Web Services Ratchet

Internal auto service management system for a small workshop.

The project is being built as a pragmatic MVP for real workshop usage and as a backend / full-stack portfolio project.

## Current features

### Backend
- Clients
  - create
  - update
  - get by id
  - list
  - search by name and phone
  - details with linked vehicles

- Vehicles
  - create for a client
  - update
  - get by id
  - search by license plate and VIN
  - details with owner and visits

- Visits
  - create for a vehicle
  - update
  - get by id
  - details with service items and total amount
  - update status

- Visit service items
  - create
  - list by visit
  - delete

### Frontend
Blazor Web UI currently includes:
- clients list with search
- client details page
- client create/edit page
- vehicle details page
- vehicle create/edit page
- visit details page
- visit create/edit page

## Tech stack
- ASP.NET Core Web API
- Blazor Web App
- PostgreSQL
- Entity Framework Core
- EF Core migrations
- Docker Compose

## Project structure
- WerkonWebServicesRatchet — backend API
- WerkonWebServicesRatchet.Web — Blazor frontend

## Running locally

### 1. Start PostgreSQL in Docker

From the repository root:

//```bash
//docker compose up -d

### 2. Apply database migrations

Using Visual Studio Package Manager Console:

    Update-Database

### 3. Run both projects

Set both projects as startup projects in Visual Studio:
- WerkonWebServicesRatchet
- WerkonWebServicesRatchet.Web

The backend API and Blazor UI will start together.

## Local configuration

The backend uses PostgreSQL connection settings from appsettings.json.

Example development connection string:

    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Port=5432;Database=ratchet_db;Username=postgres;Password=postgres"
    }

The Blazor frontend currently calls the local backend API through RatchetApiClient.

## Main API endpoints

### Clients
- GET /api/clients
- GET /api/clients/{id}
- GET /api/clients/{id}/details
- POST /api/clients
- PUT /api/clients/{id}

### Vehicles
- GET /api/vehicles
- GET /api/vehicles/{id}
- GET /api/vehicles/{id}/details
- GET /api/clients/{clientId}/vehicles
- POST /api/clients/{clientId}/vehicles
- PUT /api/vehicles/{id}

### Visits
- GET /api/visits/{id}
- GET /api/visits/{id}/details
- GET /api/vehicles/{vehicleId}/visits
- POST /api/vehicles/{vehicleId}/visits
- PUT /api/visits/{id}
- PATCH /api/visits/{id}/status

### Visit service items
- GET /api/visits/{visitId}/service-items
- POST /api/visits/{visitId}/service-items
- DELETE /api/visits/{visitId}/service-items/{itemId}

## Notes

This is an MVP-oriented project.

The current focus is:
- clear domain structure
- working CRUD and navigation flow
- workshop-friendly workflow
- practical full-stack demo

## Planned next steps
- Today page
- Schedule page
- reminders
- add/edit service items through UI
- better validation and polish
- mobile-friendly improvements
