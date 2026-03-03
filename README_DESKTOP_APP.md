# Documentation Notice
Canonical project documentation starts at `DOCUMENTATION.md` and runtime start is `START.cmd`.
This file is retained as supplemental implementation reference.

# Hecateon Desktop

Desktop UI for chat, device security, and health logging.

## Configuration
Create `config.json` beside desktop executable (or use env vars):

```json
{
  "ApiBaseUrl": "https://localhost:5001",
  "RequestTimeoutSeconds": 15,
  "DeviceId": "desktop-mydevice",
  "DeviceSecret": "replace-with-strong-random-secret",
  "RecoveryCode": "CHANGE_THIS_RECOVERY_CODE"
}
```

Environment alternatives:
- `HECATEON_API_BASE_URL`
- `HECATEON_REQUEST_TIMEOUT_SECONDS`
- `HECATEON_DEVICE_ID`
- `HECATEON_DEVICE_SECRET`
- `HECATEON_RECOVERY_CODE`

## Runtime flow
1. Desktop loads config.
2. Desktop enrolls and gets challenge.
3. Desktop signs challenge with device secret.
4. Server verifies signature and issues bearer token.
5. Desktop uses bearer token for protected API calls.

## Build & run
- `dotnet run --project Lullaby.Desktop/Lullaby.Desktop.csproj`

## Architecture
- UI: `MainWindow.xaml`
- ViewModels: `ViewModels/MainViewModel.cs`, `ViewModels/ChatViewModel.cs`, `ViewModels/DeviceViewModel.cs`, `ViewModels/HealthViewModel.cs`
- Commands: `ViewModels/AsyncRelayCommand.cs` (async command execution used by UI actions)
- API layer: `Services/DesktopApiService.cs`
- Shared client contracts/api: `Hecateon.Client`