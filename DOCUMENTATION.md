# Hecateon Core Documentation (Canonical)

This is the single source of truth for project docs.
Use `START.cmd` as the only runtime entrypoint.

## Product Direction
- Local-first, privacy-first, safety-first architecture
- Home server is source of truth with append-only event store
- Offline-capable encrypted clients
- Explainable risk scoring and deterministic intervention ladder
- Explicit consent boundaries and clinician-ready reporting

## Current MVP Scope
- Event log + sync
- Sleep / mood / routine tracking
- Explainable trend scoring
- Downshift / crisis UI
- Weekly export
- Trusted remote access

## Runtime Entry
- `START.cmd` launches backend then desktop client.

## Auth Flow
1. Device enroll (`/api/hecateon/device/enroll`)
2. Server returns challenge
3. Client signs challenge with device secret
4. Client exchanges signature for bearer token (`/api/hecateon/device/token`)
5. Protected APIs require `Authorization: Bearer ...`

## Key API Families
- `/api/chat/*`
- `/api/health/*`
- `/api/reports/*`
- `/api/hecateon/*`
- `/health`

## Build/Test
- Restore/build: `dotnet restore .\Lullaby.slnx` then `dotnet build .\Lullaby.slnx -c Release`
- Tests: `dotnet test .\Lullaby\Lullaby.Tests\Lullaby.Tests.csproj -c Release`

## Configuration
- Server config: `Lullaby/Lullaby/appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`
- Env template: `.env.example`
- Desktop config template: `config.example.json`

## Release Process
- CI workflow: `.github/workflows/ci.yml`
- Checklist: `RELEASE_READINESS_CHECKLIST.md`
- Changelog: `CHANGELOG.md`

## Legacy Docs
Other root markdown files are supplemental/history. If conflicts exist, this file is authoritative.