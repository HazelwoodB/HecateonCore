# Hecateon Bootstrapper (Single EXE)

The bootstrapper provides one executable that runs this pipeline:

1. Pretesting (`dotnet`/`git` checks)
2. Restore + build + tests
3. Server startup (`https://localhost:5001`)
4. AI warmup (`/api/chat` best-effort)
5. Desktop app publish
6. Desktop app launch
7. GitHub clone/update when needed

## Run via batch entrypoint

```bat
RUN_HECATEON_DESKTOP.cmd
```

## Install to stable folder (recommended)

To make launching reliable when VS Code is fully closed, install to a stable local path:

```bat
INSTALL_HECATEON_STABLE.cmd
```

Optional custom path:

```bat
INSTALL_HECATEON_STABLE.cmd -InstallPath "D:\Apps\HecateonCore"
```

This script clones/updates into a persistent folder, then creates a desktop shortcut (`Hecateon Launcher.lnk`) that runs:

- `RUN_HECATEON_DESKTOP.cmd main popup`

Optional profile argument:

```bat
RUN_HECATEON_DESKTOP.cmd main
RUN_HECATEON_DESKTOP.cmd dev
RUN_HECATEON_DESKTOP.cmd offline
RUN_HECATEON_DESKTOP.cmd --help
```

Optional terminal mode argument:

```bat
RUN_HECATEON_DESKTOP.cmd main popup
RUN_HECATEON_DESKTOP.cmd main inline
```

- `popup`: opens a post-load status popup only when bootstrap succeeds
- `inline`: runs in current terminal with live append-only status output

Profile behavior:

- `main`: full pipeline with `main` branch, auto-update on, validation on, AI warmup on
- `dev`: keeps local branch state, auto-update off, validation on, AI warmup on
- `offline`: keeps local branch state, auto-update off, validation off, AI warmup off

Batch entrypoint notes:

- `RUN_HECATEON_DESKTOP.cmd` republishes the bootstrapper EXE only when launcher sources are newer (or EXE is missing), reducing startup lag.
- Default mode is `popup`, which opens a dedicated terminal window for runtime visualization.
- Bootstrapper now prints a live status matrix for each stage (`Dependencies`, `Repository`, `Validation`, `Server`, `AI Warmup`, `Publish`, `Launch`).
- Matrix statuses are colorized when terminal supports ANSI: `PASS` (green), `RUNNING/SKIPPED` (yellow), `FAIL` (red), `PENDING` (dim gray).
- Popup mode now defers matrix popup until bootstrap completes successfully.
- Inline mode defaults to append-only matrix output to avoid terminal redraw lag.
- You can override this with `disableInPlaceDashboard` or `HECATEON_DISABLE_INPLACE_DASHBOARD`.

## Build single EXE manually

```powershell
dotnet publish .\Hecateon.Bootstrapper\Hecateon.Bootstrapper.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Published executable:

- `Hecateon.Bootstrapper\bin\Release\net8.0\win-x64\publish\Hecateon.Bootstrapper.exe`

## Configure without recompiling

You can configure runtime behavior using either:

1. `bootstrapper.settings.json` (next to the EXE, or in current working directory)
2. Environment variables (override JSON values)

### JSON settings file

Default example (`Hecateon.Bootstrapper/bootstrapper.settings.json`):

```json
{
  "repoUrl": "https://github.com/HazelwoodB/HecateonCore.git",
  "repoBranch": "main",
  "serverUrl": "https://localhost:5001",
  "enableAutoUpdate": true,
  "enableValidation": true,
  "enableAiWarmup": true,
  "enableDesktopPublish": true,
  "useInMemoryDbForBootstrap": true,
  "recoveryCode": "",
  "disableInPlaceDashboard": false,
  "serverWaitSeconds": 40
}
```

### Environment variable overrides

- `HECATEON_REPO_URL`
- `HECATEON_REPO_BRANCH`
- `HECATEON_SERVER_URL`
- `HECATEON_AUTO_UPDATE` (`true/false` or `1/0`)
- `HECATEON_ENABLE_VALIDATION` (`true/false` or `1/0`)
- `HECATEON_AI_WARMUP` (`true/false` or `1/0`)
- `HECATEON_ENABLE_PUBLISH` (`true/false` or `1/0`)
- `HECATEON_USE_INMEMORY_DB` (`true/false` or `1/0`) for server startup fallback during bootstrap
- `HECATEON_RECOVERY_CODE` (optional, used as `X-Recovery-Code` for AI warmup)
- `HECATEON_DISABLE_INPLACE_DASHBOARD` (`true/false` or `1/0`)
- `HECATEON_SERVER_WAIT_SECONDS` (integer)

Environment variables take precedence over JSON when both are set.

The batch launcher profiles are implemented by setting these environment variables before invoking the bootstrapper EXE.

## Notes

- If the repository is missing at runtime, bootstrapper clones from:
  - `repoUrl` (default: `https://github.com/HazelwoodB/HecateonCore.git`)
- If the repository exists, bootstrapper attempts `git fetch` + `git pull --ff-only`.
- Bootstrapper sets `HECATEON_USE_INMEMORY_DB=true` by default, so server startup does not depend on local SQL Server/LocalDB.
- AI warmup is best-effort and does not block startup when device approval rules return non-success.
- If validation/publish/warmup are disabled, bootstrapper continues with the remaining pipeline steps.
