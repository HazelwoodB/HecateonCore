# Hecateon Desktop

Hecateon Desktop is the local operator interface for the Hecateon platform.

## What's New

✅ **Native Windows Desktop Application** - Fast, responsive, no browser dependency  
✅ **Operator Surface** - Operator Console, Command Overview, Chronicle, Projection, System Configuration  
✅ **Secure Backend** - ASP.NET Core running locally on `https://localhost:5001`  
✅ **Professional UI** - Clean, modern interface with proper styling  
✅ **Offline-Capable** - Works without internet, syncs when connected  
✅ **Encryption** - All data encrypted with AES-256-GCM  

## Quick Start

### Option 1: Batch Script (Windows)
```batch
RUN_HECATEON_DESKTOP.cmd
```

### Option 2: PowerShell Script
```powershell
.\RUN_HECATEON_DESKTOP.cmd
```

### Option 3: Manual Start
**Terminal 1 - Start Backend:**
```powershell
cd Lullaby\Lullaby
dotnet run
```

**Terminal 2 - Start Desktop App:**
```powershell
cd Lullaby\Lullaby.Desktop
dotnet run
```

## Features

### 💬 Chat Tab
- Send messages to Lullaby AI assistant
- Real-time responses from backend
- Message history loaded automatically
- Connection status indicator
- Error feedback displayed in red banner

### ❤️ Health Tab
- Log mood (Great, Good, Neutral, Poor, Very Poor)
- Track sleep hours (0-12 hours)
- Submit daily health entries
- Last entry timestamp displayed
- Data sent to secure backend

### ⚙️ Settings Tab
- **Device Information** - Unique device ID for trusted device enrollment
- **Recovery Code** - Toggle visibility, keep secure (never share!)
- **Data & Privacy** - View security features (AES-256-GCM, event-sourced, offline-capable)
- **About** - Version, architecture, framework information

## Architecture

```
┌─────────────────────────────────────────────────┐
│   Hecateon.Desktop (WPF)                       │
│   ├─ MainWindow.xaml (UI Layout)               │
│   ├─ MainWindow.xaml.cs (Chat, Health logic)   │
│   └─ HttpClient (API Communication)            │
└──────────────────┬──────────────────────────────┘
                   │ HTTPS (localhost:5001)
                   ▼
┌─────────────────────────────────────────────────┐
│   Hecateon Core Runtime (ASP.NET Core)         │
│   ├─ /api/chat - Send chat messages            │
│   ├─ /api/history - Fetch message history      │
│   ├─ /api/health/log - Log health entries      │
│   ├─ /api/nyphos/* - Risk assessment           │
│   ├─ /api/hecateon/device/* - Device mgmt      │
│   └─ Encryption Service (AES-256-GCM)          │
└─────────────────────────────────────────────────┘
```

## System Requirements

- **OS:** Windows 10 or later (64-bit)
- **.NET SDK:** .NET 8.0 or later (WPF uses .NET 8.0)
- **Backend:** .NET 10 SDK recommended
- **Memory:** 512 MB minimum
- **Disk Space:** ~500 MB for .NET runtimes

## Troubleshooting

### Connection Failed / Can't connect to server
1. Make sure terminal 1 (backend) is still running
2. Check if port 5001 is available: `netstat -ano | findstr :5001`
3. Restart the launcher script

### Backend fails to start
```powershell
# Check .NET installation
dotnet --version

# Restore NuGet packages
cd Lullaby\Lullaby
dotnet restore
dotnet build
```

### Desktop app won't launch
```powershell
# Build desktop project
cd Lullaby\Lullaby.Desktop
dotnet restore
dotnet build
dotnet run
```

### Port 5001 already in use
Edit `Lullaby/Lullaby/Program.cs` and change the URL:
```csharp
app.Urls.Add("https://localhost:5002"); // Change to different port
```

## Development

### Project Structure
```
HecateonCore/
├── Lullaby/                    # Backend (ASP.NET Core 10)
│   ├── Program.cs             # Startup, API routes
│   ├── Core/                  # Encryption, event store, device registry
│   ├── Modules/Nyphos/        # Risk assessment engine
│   └── Services/              # Business logic
│
├── Lullaby.Client/            # Blazor WASM (deprecated, kept for reference)
│
└── Hecateon.Desktop/          # Desktop App (WPF, .NET 8)
    ├── MainWindow.xaml        # UI Layout
    ├── MainWindow.xaml.cs     # Code-behind logic
    └── Lullaby.Desktop.csproj # Project file
```

### Building

Clean build:
```powershell
dotnet clean
dotnet build
```

Publish as single-file EXE (coming next):
```powershell
cd Lullaby.Desktop
dotnet publish -c Release -r win-x64 --self-contained
```

## API Endpoints

The desktop app calls these backend endpoints:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/chat` | POST | Send chat message |
| `/api/history` | GET | Get message history |
| `/api/health/log` | POST | Log health entry |
| `/api/nyphos/assess` | POST | Risk assessment |
| `/api/hecateon/device/enroll` | POST | Enroll device |
| `/api/hecateon/device/approve` | POST | Approve device |
| `/api/export` | GET | Export user data |

## Security Notes

🔒 **Your data is protected:**
- **AES-256-GCM** encryption at rest
- **Event-sourced** append-only log (immutable history)
- **Offline-capable** - works without internet
- **No cloud sync** - everything stays local by default
- **VPN-only** remote access with trusted device registry
- **PBKDF2-SHA256** password hashing (if authentication added)

⚠️ **Keep safe:**
- Never share your recovery code
- Device ID identifies your device for remote access
- Store recovery code in secure location
- Export regular backups

## Next Steps

1. ✅ **Desktop app created and working**
2. ⏳ **Package as EXE installer** (optional)
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   ```
3. ⏳ **Create smart launcher** (one-click startup)
4. ⏳ **Add WIX installer** (professional installation experience)

## Support

For issues or questions about the desktop implementation:
1. Check the launcher output logs
2. Verify both backend and desktop are running
3. Check `Lullaby/Lullaby/logs/` for backend logs
4. Review error messages in the red banner in the chat tab

## License

This project is part of Lullaby - Mental Health Companion.  
Designed with local-first, deterministic, safety-bounded principles.

---

**Ready to use!** Run `RUN_HECATEON_DESKTOP.cmd` to start.
