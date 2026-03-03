# Documentation Notice
Canonical project documentation starts at `DOCUMENTATION.md` and runtime start is `START.cmd`.
This file remains the release verification artifact template.

# Release Readiness Checklist

Use this checklist on a **full local clone** (not RemoteHub cache) to verify final build state before release.

---

## 0) Environment Gate

- [ ] Open repo from a real local path (example: `C:\Users\hazel\source\repos\HecateonCore`)
- [ ] Confirm solution and projects exist on disk:
  - [ ] `Lullaby.slnx`
  - [ ] `Lullaby\Lullaby\Lullaby.csproj`
  - [ ] `Lullaby\Lullaby.Client\Lullaby.Client.csproj`
  - [ ] `Lullaby.Desktop\Lullaby.Desktop.csproj`
  - [ ] `Lullaby\Lullaby.Tests\Lullaby.Tests.csproj`

### Verify in PowerShell
```powershell
Test-Path .\Lullaby.slnx
Test-Path .\Lullaby\Lullaby\Lullaby.csproj
Test-Path .\Lullaby\Lullaby.Client\Lullaby.Client.csproj
Test-Path .\Lullaby.Desktop\Lullaby.Desktop.csproj
Test-Path .\Lullaby\Lullaby.Tests\Lullaby.Tests.csproj
```

Pass condition: all return `True`.

---

## 1) Restore + Build Gate

```powershell
dotnet restore .\Lullaby.slnx
dotnet build .\Lullaby.slnx -c Release -v minimal
```

Pass condition:
- Build succeeds (`0` exit code)
- No errors
- Warnings are acceptable only if reviewed and explicitly accepted

---

## 2) Contract Test Gate

```powershell
dotnet test .\Lullaby\Lullaby.Tests\Lullaby.Tests.csproj -c Release -v minimal
```

Pass condition:
- All tests pass
- `Failed: 0`

---

## 3) Desktop UX Smoke Gate

Run backend + desktop:

```powershell
# Terminal 1
cd .\Lullaby\Lullaby
dotnet run

# Terminal 2
cd .\Lullaby.Desktop
dotnet run
```

Manual checks:
- [ ] Chat send works
- [ ] Chat Send button and Ctrl+Enter both trigger command path
- [ ] Health save works and updates inline status text
- [ ] Mood selection shows selected visual state
- [ ] Export weekly report shows inline status text
- [ ] Recovery code status helper updates on Show/Hide
- [ ] No direct UI click-handler regressions (actions execute via ViewModel commands)
- [ ] Keyboard focus ring visible on inputs/buttons/tabs

---

## 4) API/Auth Gate

- [ ] Enrollment returns challenge (`/api/hecateon/device/enroll`)
- [ ] Device signs challenge; token exchange succeeds (`/api/hecateon/device/token`)
- [ ] Protected endpoints reject missing/invalid `Authorization: Bearer ...`
- [ ] Recovery-code endpoints reject missing/invalid `X-Recovery-Code`
- [ ] Canonical endpoints in use (`/api/health/events`, `/api/reports/weekly`, `/api/hecateon/*`)

---

## 5) Docs Consistency Gate

- [ ] `README_DESKTOP_APP.md` matches modular endpoint architecture
- [ ] `HECATEONCORE_PROJECT_OVERVIEW.md` reflects endpoint modules + DTO ownership
- [ ] No stale endpoint references (`/api/export`, `/api/nyphos/assess`, `/api/devices/*`)

Quick scan:
```powershell
Select-String -Path .\*.md, .\**\*.md -Pattern '/api/export|/api/nyphos/assess|/api/devices/'
```

Pass condition: no results (or only intentional historical notes).

---

## 6) Packaging Gate (Optional)

```powershell
cd .\Lullaby.Desktop
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Pass condition:
- Publish succeeds
- Output artifact created in `bin\Release\net8.0-windows\win-x64\publish\`

---

## Current Known Constraint (This Session)

In this VS Code RemoteHub cache environment, physical project files are partially absent at runtime paths, which blocks trustworthy terminal `dotnet build/test` execution despite clean editor diagnostics. Run the checklist above in a full local clone to produce final release evidence.

---

## Final Sign-Off

- [ ] Build gate passed
- [ ] Test gate passed
- [ ] UX smoke passed
- [ ] Auth/API gate passed
- [ ] Docs gate passed
- [ ] (Optional) publish gate passed
- [ ] Release candidate approved

---

## Release Evidence Template

Use this section to capture auditable release proof for each candidate.

### Metadata
- Release version/tag: `v1.0.0`
- Date/time (UTC):
- Operator:
- Commit SHA:

### Build Evidence
- `dotnet restore` result:
- `dotnet build -c Release` result:
- Artifact paths:

### Test Evidence
- `dotnet test` summary (`Passed/Failed/Skipped`):
- Test report location (if exported):

### Auth Flow Evidence
- Enroll challenge captured (`/api/hecateon/device/enroll`): yes/no
- Token exchange captured (`/api/hecateon/device/token`): yes/no
- Protected endpoint unauthorized without bearer token: pass/fail
- Protected endpoint authorized with bearer token: pass/fail

### Desktop Smoke Evidence
- Chat send (button + Ctrl+Enter): pass/fail
- Health save + inline status: pass/fail
- Mood selection visual state: pass/fail
- Weekly report export: pass/fail

### Observability Evidence
- `/health` response captured: yes/no
- Correlation ID seen in response/log pair: yes/no
- ProblemDetails payload verified for non-200 path: yes/no

### Release Decision
- Decision: `APPROVED` / `BLOCKED`
- Blocking issues (if any):
- Follow-up ticket(s):