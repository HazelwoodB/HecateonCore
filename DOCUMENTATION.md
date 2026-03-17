# Hecateon Core Documentation (Canonical)

This is the single source of truth for project docs.
Use `START.cmd` as the only runtime entrypoint.

## Playbook
- One-file implementation and architecture source: `HECATEON_ONE_FILE_PLAYBOOK.md`

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
# HecateonCore Next Steps Playbook

## 1. Security & Privacy Hardening
- Enforce end-to-end encryption for all client-server and backup operations.
- Implement robust audit logging for all operator and system actions.
- Expand trusted device registry with device attestation and revocation.
- Require explicit user consent for all remote or sensitive actions.

## 2. Data Integrity & Resilience
- Make event store and all critical logs append-only and tamper-evident.
- Add automated, encrypted, versioned backup/restore with integrity checks.
- Implement offline-first sync with conflict resolution and rollback.

## 3. Risk Engine & Explainability
- Expand risk scoring to cover more event types and user routines.
- Add interpretable, clinician-friendly explanations for all risk outputs.
- Version all rules/models and support rollback to previous versions.

## 4. Operator & Clinician Experience
- Refine operator panel for rapid triage, downshift, and crisis workflows.
- Add customizable intervention ladders and consent boundaries.
- Implement weekly/monthly export with full audit and trend reporting.

## 5. Remote Access & Federation
- Restrict remote access to VPN and trusted devices only.
- Add federated sync for multi-site/home deployments (opt-in).
- Provide clear UI for remote session consent and revocation.

## 6. Testing, Monitoring, & Compliance
- Expand automated test coverage (unit, integration, security, UI).
- Add real-time health monitoring and alerting for all core services.
- Prepare compliance documentation for privacy, safety, and audit standards.

## 7. Documentation & Onboarding
- Update all user, operator, and developer documentation.
- Provide onboarding guides for clinicians and home users.
- Maintain a clear, versioned changelog and upgrade path.