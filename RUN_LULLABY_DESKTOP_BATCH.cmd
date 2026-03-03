@echo off
setlocal enabledelayedexpansion

set "LULLABY_ROOT=%USERPROFILE%\source\repos\Lullaby"
set "DESKTOP_DIR=%LULLABY_ROOT%\Lullaby.Desktop"

if not exist "%DESKTOP_DIR%" (
    echo ERROR: Desktop app not found at %DESKTOP_DIR%
    pause
    exit /b 1
)

cd /d "%DESKTOP_DIR%"

echo.
echo ========================================
echo    LULLABY - Launching...
echo ========================================
echo.

dotnet run --configuration Debug

endlocal
