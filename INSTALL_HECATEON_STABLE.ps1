param(
    [string]$InstallPath = "C:\HecateonCore",
    [string]$RepoUrl = "https://github.com/HazelwoodB/HecateonCore.git",
    [switch]$NoDesktopShortcut
)

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }

Write-Host "========================================"
Write-Host "HECATEON STABLE INSTALLER"
Write-Host "========================================"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "git is required but not found in PATH."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK is required but not found in PATH."
}

$resolvedInstallPath = [System.IO.Path]::GetFullPath($InstallPath)
$parentPath = Split-Path -Parent $resolvedInstallPath
if (-not (Test-Path $parentPath)) {
    New-Item -ItemType Directory -Path $parentPath -Force | Out-Null
}

$gitPath = Join-Path $resolvedInstallPath ".git"
if (-not (Test-Path $gitPath)) {
    if (Test-Path $resolvedInstallPath) {
        Write-Warn "Install path exists but is not a git repository. Replacing it: $resolvedInstallPath"
        Remove-Item -Recurse -Force $resolvedInstallPath
    }

    Write-Info "Cloning repository to $resolvedInstallPath"
    git clone $RepoUrl $resolvedInstallPath
} else {
    Write-Info "Updating existing repository at $resolvedInstallPath"
    Push-Location $resolvedInstallPath
    try {
        git fetch --all --prune
        git pull --ff-only
    } finally {
        Pop-Location
    }
}

$launcherPath = Join-Path $resolvedInstallPath "RUN_HECATEON_DESKTOP.cmd"
if (-not (Test-Path $launcherPath)) {
    throw "Launcher not found at $launcherPath"
}

if (-not $NoDesktopShortcut) {
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "Hecateon Launcher.lnk"

    $wsh = New-Object -ComObject WScript.Shell
    $shortcut = $wsh.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $launcherPath
    $shortcut.Arguments = "main popup"
    $shortcut.WorkingDirectory = $resolvedInstallPath
    $shortcut.Description = "Launch Hecateon"
    $shortcut.Save()

    Write-Ok "Desktop shortcut created: $shortcutPath"
}

Write-Host ""
Write-Ok "Stable install ready."
Write-Host "Install path: $resolvedInstallPath"
Write-Host "Run: $launcherPath"
Write-Host ""
Write-Host "Tip: You can now close VS Code and launch from the desktop shortcut."
