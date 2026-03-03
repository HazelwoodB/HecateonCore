# HecateonCore - Lullaby Desktop Application
## Comprehensive Project Overview

**Repository**: `https://github.com/HazelwoodB/HecateonCore.git`  
**Branch**: `main`  
**Project Name**: HecateonCore Platform  
**Type**: WPF Desktop Application (.NET) + ASP.NET Core Web Dashboard

---

## 📜 Project History

**HecateonCore** is the evolution and consolidation of the AutoPC project. All development has been unified into this repository as of **January 2025**.

### Migration Timeline:
- **Q4 2024**: AutoPC web application (original proof-of-concept)
- **Q1 2025**: Expanded to include Hecateon Desktop (WPF), NYPHOS module, comprehensive tooling
- **January 2025**: Migrated to HecateonCore repository, AutoPC archived

**Previous Repository**: [AutoPC](https://github.com/HazelwoodB/AutoPC) (Archived - Read-only)

---

## 🎯 Project Mission

**Local-first, Privacy-first, Safety-first** architecture for personal wellness monitoring with predictable behavior and strong user agency.

### Core Principles
- **Home server as source of truth** with append-only event store
- **Offline-capable encrypted clients**
- **VPN-only remote access** with trusted device registry
- **Interpretable risk engine** with explainable outputs and hysteresis
- **Deterministic intervention ladder**
- **Explicit consent boundaries**
- **Clinician-ready reporting**
- **Encrypted backups**
- **Versioned rules/models**

---

## 🏗️ Architecture

### System Components

#### **1. Lullaby Desktop Application** (WPF)
- Main UI for personal wellness tracking
- Local-first data storage
- Real-time monitoring and visualization
- Crisis intervention UI
- Located: `Lullaby.Desktop/`

#### **2. Lullaby Web Application** (ASP.NET Core)
- Web-based interface
- Bootstrap 5 UI framework
- RESTful API endpoints
- Located: `Lullaby/Lullaby/`

#### **3. Launcher System**
Multiple entry points for ease of use:
- `START_LULLABY_DESKTOP.cmd` (Desktop shortcut)
- `RUN_LULLABY_DESKTOP.cmd` (PowerShell wrapper)
- `RUN_LULLABY_DESKTOP.ps1` (Direct PowerShell)
- `RUN_LULLABY_DESKTOP_BATCH.cmd` (Batch file)
- `RUN_LULLABY_WITH_VALIDATION.cmd` (With system checks)

#### **4. Validation System**
- `CHECK_SYSTEM.cmd` - Pre-flight system validation
- Checks .NET SDK, dependencies, and environment

---

## 📊 MVP Focus Areas

### Phase 1: Core Infrastructure ✅
- [x] Event log + sync system
- [x] Append-only event store
- [x] Offline-capable clients
- [x] Encrypted local storage

### Phase 2: Wellness Features ✅ (NYPHOS Complete)
- [x] Sleep tracking
- [x] Mood monitoring
- [x] Daily routines
- [x] Habit tracking
- [x] Explainable trend scoring

### Phase 3: Safety Systems
- [x] Downshift/crisis UI
- [x] Risk engine with explainability
- [x] Intervention ladder
- [ ] Weekly export for clinicians
- [ ] Trusted remote access

---

## 🛠️ Technology Stack

### Frontend
- **WPF** (Windows Presentation Foundation)
- **XAML** for UI markup
- **C#** for code-behind
- **Bootstrap 5** (web version)

### Backend
- **ASP.NET Core** web framework
- **.NET 8.0** SDK
- **Entity Framework Core** (data access)

### Data Storage
- **Local-first** architecture
- **Encrypted SQLite** databases
- **Append-only event store**

### Development Tools
- **Visual Studio 2022**
- **Git** for version control
- **PowerShell** for automation
- **Batch scripts** for launchers

---

## 📁 Project Structure

```
Lullaby/
├── .github/
│   └── copilot-instructions.md          # Development guidelines
├── Lullaby.Desktop/                      # WPF Desktop Application
│   ├── MainWindow.xaml                   # Main UI layout
│   ├── MainWindow.xaml.cs                # UI logic
│   ├── App.xaml                          # Application resources
│   ├── Lullaby.Desktop.csproj            # Project file
│   └── REFINEMENT_NOTES.md               # Development notes
├── Lullaby/                              # Web Application
│   └── Lullaby/
│       ├── Controllers/                  # API controllers
│       ├── Models/                       # Data models
│       ├── Views/                        # Razor views
│       ├── wwwroot/                      # Static assets
│       │   └── lib/bootstrap/            # Bootstrap 5 framework
│       └── Lullaby.csproj                # Web project file
├── Documentation/
│   ├── HECATEONCORE_PROJECT_OVERVIEW.md  # This file
│   ├── README_DESKTOP_APP.md             # Desktop app guide
│   ├── NYPHOS.md                         # NYPHOS wellness system
│   ├── ARCHITECTURE_REFINED.md           # Architecture details
│   ├── WELLNESS_FEATURES.md              # Feature documentation
│   └── COMPREHENSIVE_REFINEMENT_SUMMARY.md
├── Launchers/
│   ├── START_LULLABY_DESKTOP.cmd         # Quick start (Desktop)
│   ├── RUN_LULLABY_DESKTOP.cmd           # Main launcher
│   ├── RUN_LULLABY_DESKTOP.ps1           # PowerShell version
│   ├── RUN_LULLABY_DESKTOP_BATCH.cmd     # Batch version
│   └── RUN_LULLABY_WITH_VALIDATION.cmd   # With validation
├── Tools/
│   ├── CHECK_SYSTEM.cmd                  # System validator
│   ├── CHANGE_REMOTE_TO_HECATEON.cmd     # Git remote changer
│   └── TEST_CHAT_PRELOAD.ps1             # Testing utilities
├── Lullaby.slnx                          # Solution file
└── README.md                             # Main README
```

---

## 🚀 Quick Start Guide

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK or later
- Visual Studio 2022 (recommended)
- Git (for version control)

### Launch Desktop Application

**Option 1: Double-click Shortcut**
```
Desktop\START_LULLABY_DESKTOP.cmd
```

**Option 2: From Project Directory**
```cmd
cd %USERPROFILE%\source\repos\Lullaby
RUN_LULLABY_DESKTOP_BATCH.cmd
```

**Option 3: With Validation**
```cmd
cd %USERPROFILE%\source\repos\Lullaby
RUN_LULLABY_WITH_VALIDATION.cmd
```

**Option 4: PowerShell**
```powershell
cd $env:USERPROFILE\source\repos\Lullaby
.\RUN_LULLABY_DESKTOP.ps1
```

### Build from Source

**Using Visual Studio:**
1. Open `Lullaby.slnx`
2. Set `Lullaby.Desktop` as startup project
3. Press F5 to build and run

**Using Command Line:**
```cmd
cd %USERPROFILE%\source\repos\Lullaby\Lullaby.Desktop
dotnet build
dotnet run
```

---

## 🎨 Key Features

### Desktop Application (Lullaby.Desktop)

#### 1. **Personal Dashboard**
- Real-time wellness metrics
- Visual trend charts
- Quick status indicators

#### 2. **Event Logging**
- Append-only event store
- Timestamped entries
- Encrypted local storage
- Offline-first design

#### 3. **Sleep Tracking**
- Sleep duration monitoring
- Quality assessment
- Pattern analysis
- Historical trends

#### 4. **Mood Monitoring**
- Mood logging with timestamps
- Trend visualization
- Pattern recognition
- Correlation analysis

#### 5. **Routine Management**
- Daily routine tracking
- Habit formation support
- Progress visualization
- Reminder system

#### 6. **Risk Assessment**
- Explainable risk scoring
- Hysteresis to prevent oscillation
- Deterministic intervention ladder
- Crisis detection

#### 7. **Privacy Features**
- Local-first storage
- End-to-end encryption
- VPN-only remote access
- Trusted device registry
- Explicit consent for data sharing

#### 8. **Clinical Export**
- Weekly summary generation
- Clinician-ready reports
- Privacy-preserving aggregation
- Export to standard formats

---

## 🔒 Security & Privacy

### Data Protection
- **Local-first**: All data stored locally by default
- **Encrypted at rest**: SQLite database encryption
- **Encrypted in transit**: TLS for any network communication
- **No cloud sync without consent**: Explicit user permission required

### Access Control
- **VPN-only remote access**: Home VPN required for remote connections
- **Trusted device registry**: Whitelist of approved devices
- **Multi-factor authentication**: Optional MFA for sensitive actions
- **Session management**: Auto-logout after inactivity

### Audit Trail
- **Append-only logs**: Cannot modify historical data
- **Versioned models**: Track changes to risk algorithms
- **Access logs**: Record all data access attempts
- **Consent logs**: Track all consent decisions

---

## 📈 Development Status

### ✅ Completed Phases

#### **Phase 1: Foundation** (Complete)
- Desktop application scaffolding
- Basic UI framework
- Event store implementation
- Local database setup

#### **Phase 2: NYPHOS Wellness** (Complete)
- Sleep tracking module
- Mood monitoring system
- Routine management
- Habit tracking
- Trend analysis

#### **Phase 3: Testing & Refinement** (Complete)
- Comprehensive testing suite
- Multiple launcher systems
- Validation scripts
- Documentation complete

### 🚧 In Progress

#### **Phase 4: Safety Systems**
- Risk engine refinement
- Intervention ladder implementation
- Crisis UI enhancement
- Clinical export functionality

### 📋 Planned Features

#### **Phase 5: Remote Access**
- VPN integration
- Trusted device management
- Secure sync protocol
- Remote monitoring dashboard

#### **Phase 6: Clinical Integration**
- FHIR export support
- Standard report formats
- Clinician portal
- Consent management UI

---

## 🧪 Testing

### Manual Testing
```cmd
RUN_LULLABY_WITH_VALIDATION.cmd
```

### System Validation
```cmd
CHECK_SYSTEM.cmd
```

### Test Chat Preload
```powershell
.\TEST_CHAT_PRELOAD.ps1
```

### Build Verification
```cmd
dotnet build
dotnet test
```

---

## 📚 Documentation Index

### Getting Started
- `README_DESKTOP_APP.md` - Desktop application guide
- `QUICK_START.md` - Quick start guide
- `00_START_HERE.md` - New developer onboarding

### Architecture
- `ARCHITECTURE_REFINED.md` - System architecture
- `NYPHOS.md` - NYPHOS wellness system design
- `.github/copilot-instructions.md` - Development guidelines

### Features
- `WELLNESS_FEATURES.md` - Feature documentation
- `NYPHOS_IMPLEMENTATION.md` - Implementation details
- `VISUAL_REFERENCE.md` - UI/UX reference

### Completion Reports
- `PROJECT_COMPLETE.md` - Overall project status
- `DESKTOP_COMPLETION_REPORT.txt` - Desktop app completion
- `NYPHOS_PHASE2_COMPLETE.md` - Phase 2 completion
- `COMPREHENSIVE_REFINEMENT_SUMMARY.md` - Refinement summary
- `VERIFICATION_COMPLETE.md` - Verification report

### Launcher Documentation
- `README_LAUNCHER_SYSTEM.md` - Launcher system overview
- `LAUNCHER_SUMMARY.md` - Launcher feature summary
- `POWERSHELL_LAUNCHER_CODE.md` - PowerShell implementation

### Testing
- `TESTING_REPORT.md` - Test results
- `NYPHOS_TESTING_GUIDE.md` - Testing guide

---

## 🔄 Git Workflow

### Repository Information
- **Remote**: `https://github.com/HazelwoodB/HecateonCore.git`
- **Branch**: `main`
- **Local Path**: `C:\Users\hazel\source\repos\Lullaby`

### Common Commands

**Check Status:**
```bash
git status
```

**Stage Changes:**
```bash
git add .
```

**Commit Changes:**
```bash
git commit -m "Description of changes"
```

**Push to GitHub:**
```bash
git push origin main
```

**Pull Latest:**
```bash
git pull origin main
```

**View Remote:**
```bash
git remote -v
```

**Change Remote:**
```bash
git remote set-url origin <new-url>
```

---

## 🛡️ Development Guidelines

From `.github/copilot-instructions.md`:

### Core Principles
1. **Local-first architecture** - Data lives on user's device
2. **Privacy by design** - Minimize data collection
3. **Safety first** - Predictable, deterministic behavior
4. **User agency** - User controls their data
5. **Explainability** - All decisions must be explainable

### Code Standards
- **Append-only storage** - Never modify historical data
- **Versioned models** - Track algorithm changes
- **Deterministic logic** - Same input = same output
- **Hysteresis** - Prevent rapid oscillation
- **Explicit consent** - User approves all sharing

### Testing Requirements
- Unit tests for all business logic
- Integration tests for data flows
- UI tests for critical paths
- Manual testing before releases

---

## 🌐 Technology References

### .NET & WPF
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [XAML Overview](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/xaml/)

### ASP.NET Core
- [ASP.NET Core Docs](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Razor Pages](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/)

### Security
- [.NET Cryptography](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [SQLite Encryption](https://www.sqlite.org/see/doc/trunk/www/readme.wiki)
- [OWASP Guidelines](https://owasp.org/)

---

## 🎯 Project Milestones

### Milestone 1: Foundation ✅
- Desktop application created
- Basic UI implemented
- Local storage configured
- **Completed**: Q4 2024

### Milestone 2: NYPHOS Wellness ✅
- Sleep tracking
- Mood monitoring
- Routine management
- **Completed**: Q1 2025

### Milestone 3: Testing & Launchers ✅
- Comprehensive testing
- Multiple launcher options
- System validation
- **Completed**: Q1 2025

### Milestone 4: Safety Systems 🚧
- Risk engine
- Intervention ladder
- Crisis UI
- **Target**: Q2 2025

### Milestone 5: Remote Access 📋
- VPN integration
- Secure sync
- Trusted devices
- **Target**: Q3 2025

### Milestone 6: Clinical Integration 📋
- Export functionality
- Clinician portal
- Standard formats
- **Target**: Q4 2025

---

## 👥 Team & Contact

### Project Lead
- **GitHub**: [@HazelwoodB](https://github.com/HazelwoodB)
- **Repository**: [HecateonCore](https://github.com/HazelwoodB/HecateonCore)

### Contributing
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Code of Conduct
- Respect privacy-first principles
- Follow security best practices
- Write clear, maintainable code
- Document all changes
- Test thoroughly before committing

---

## 📊 Project Statistics

### Codebase
- **Total Files**: 275+
- **Languages**: C#, XAML, PowerShell, Batch
- **Frameworks**: WPF, ASP.NET Core
- **Size**: ~944 KiB

### Development
- **Active Branches**: `main`
- **Latest Commit**: Initial HecateonCore commit
- **Development Environment**: Visual Studio 2022
- **Target Framework**: .NET 8.0

### Documentation
- **Markdown Files**: 50+
- **Code Comments**: Extensive inline documentation
- **README Files**: Multiple specialized guides
- **API Documentation**: In-progress

---

## 🔍 Troubleshooting

### Common Issues

#### Desktop App Won't Launch
1. Check .NET SDK: `dotnet --version`
2. Verify project path: `cd %USERPROFILE%\source\repos\Lullaby`
3. Run validation: `CHECK_SYSTEM.cmd`
4. Check logs in Output window

#### Build Errors
1. Clean solution: `dotnet clean`
2. Restore packages: `dotnet restore`
3. Rebuild: `dotnet build`
4. Check Visual Studio output window

#### Git Issues
1. Verify remote: `git remote -v`
2. Check credentials: `git config --list`
3. Reset if needed: `git reset --hard origin/main`
4. Use Git Bash for complex operations

### Getting Help
1. Check documentation in project root
2. Review `TROUBLESHOOTING.md` (if exists)
3. Check GitHub Issues
4. Contact project maintainer

---

## 📝 License

**Proprietary** - All rights reserved. Contact project owner for licensing information.

---

## 🎉 Acknowledgments

- **Microsoft** - .NET Framework and Visual Studio
- **WPF Community** - UI framework support
- **ASP.NET Team** - Web framework
- **Bootstrap** - CSS framework
- **GitHub** - Version control and hosting

---

## 🔮 Future Vision

### Year 1: Core Stability
- Robust local-first operation
- Comprehensive safety features
- Clinical export functionality
- Multi-device support

### Year 2: Ecosystem Growth
- Mobile companion app
- Web dashboard
- API for third-party integrations
- Community plugins

### Year 3: Advanced Features
- AI-assisted trend analysis
- Predictive risk modeling
- Integration with wearables
- Telemedicine integration

---

## 📞 Support

### Documentation
- Project Wiki: (In development)
- GitHub Discussions: [HecateonCore Discussions](https://github.com/HazelwoodB/HecateonCore/discussions)

### Issues
- Bug Reports: [GitHub Issues](https://github.com/HazelwoodB/HecateonCore/issues)
- Feature Requests: [GitHub Issues](https://github.com/HazelwoodB/HecateonCore/issues)

### Community
- Discord: (Coming soon)
- Mailing List: (Coming soon)

---

## 📅 Changelog

### v1.0.0 - Initial HecateonCore Release (2025-01-XX)
- ✅ Desktop application complete
- ✅ NYPHOS wellness features
- ✅ Multiple launcher options
- ✅ System validation tools
- ✅ Comprehensive documentation
- ✅ Git repository migrated to HecateonCore

### Previous Versions (AutoPC)
- See AutoPC repository for historical changes

---

**Last Updated**: January 2025  
**Document Version**: 1.0  
**Status**: Active Development

---

*For the latest updates, visit the [HecateonCore GitHub Repository](https://github.com/HazelwoodB/HecateonCore)*
