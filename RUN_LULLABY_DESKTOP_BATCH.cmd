@echo off
setlocal enabledelayedexpansion

set "REPO_ROOT=%~dp0"
set "BOOTSTRAPPER_PROJ=%REPO_ROOT%Hecateon.Bootstrapper\Hecateon.Bootstrapper.csproj"
set "BOOTSTRAPPER_EXE=%REPO_ROOT%Hecateon.Bootstrapper\bin\Release\net8.0\win-x64\publish\Hecateon.Bootstrapper.exe"
set "PROFILE=%~1"
set "MODE=%~2"

if "%PROFILE%"=="" set "PROFILE=main"
if "%MODE%"=="" set "MODE=popup"

if /i "%PROFILE%"=="--help" goto :show_help
if /i "%PROFILE%"=="-h" goto :show_help
if /i "%PROFILE%"=="help" goto :show_help

if /i "%MODE%"=="--inline" set "MODE=inline"
if /i "%MODE%"=="inline" set "MODE=inline"
if /i "%MODE%"=="--popup" set "MODE=popup"
if /i "%MODE%"=="popup" set "MODE=popup"

if /i not "%MODE%"=="popup" if /i not "%MODE%"=="inline" (
    echo ERROR: Unknown mode "%MODE%".
    call :print_usage
    pause
    exit /b 1
)

if /i "%PROFILE%"=="main" goto :set_main
if /i "%PROFILE%"=="dev" goto :set_dev
if /i "%PROFILE%"=="offline" goto :set_offline

echo ERROR: Unknown profile "%PROFILE%".
call :print_usage
pause
exit /b 1

:set_main
set "HECATEON_REPO_BRANCH=main"
set "HECATEON_AUTO_UPDATE=true"
set "HECATEON_ENABLE_VALIDATION=true"
set "HECATEON_AI_WARMUP=true"
set "HECATEON_ENABLE_PUBLISH=true"
set "HECATEON_SERVER_URL=https://localhost:5001"
set "HECATEON_SERVER_WAIT_SECONDS=40"
goto :profile_ready

:set_dev
set "HECATEON_REPO_BRANCH="
set "HECATEON_AUTO_UPDATE=false"
set "HECATEON_ENABLE_VALIDATION=true"
set "HECATEON_AI_WARMUP=true"
set "HECATEON_ENABLE_PUBLISH=true"
set "HECATEON_SERVER_URL=https://localhost:5001"
set "HECATEON_SERVER_WAIT_SECONDS=40"
goto :profile_ready

:set_offline
set "HECATEON_REPO_BRANCH="
set "HECATEON_AUTO_UPDATE=false"
set "HECATEON_ENABLE_VALIDATION=false"
set "HECATEON_AI_WARMUP=false"
set "HECATEON_ENABLE_PUBLISH=true"
set "HECATEON_SERVER_URL=https://localhost:5001"
set "HECATEON_SERVER_WAIT_SECONDS=40"
goto :profile_ready

:profile_ready

if not exist "%BOOTSTRAPPER_PROJ%" (
    echo ERROR: Bootstrapper project not found at %BOOTSTRAPPER_PROJ%
    pause
    exit /b 1
)

cd /d "%REPO_ROOT%"

echo.
echo ========================================
echo    HECATEON BOOTSTRAPPER - Launching...
echo ========================================
echo Profile: %PROFILE%
echo.

set "NEED_PUBLISH=true"
for /f %%I in ('powershell -NoProfile -Command "$exe = '%BOOTSTRAPPER_EXE%'; $proj = '%BOOTSTRAPPER_PROJ%'; $program = '%REPO_ROOT%Hecateon.Bootstrapper\Program.cs'; $settings = '%REPO_ROOT%Hecateon.Bootstrapper\bootstrapper.settings.json'; if (!(Test-Path $exe)) { 'true' } else { $exeTime = (Get-Item $exe).LastWriteTimeUtc; $sources = @($proj, $program, $settings) | Where-Object { Test-Path $_ }; $maxSourceTime = ($sources | ForEach-Object { (Get-Item $_).LastWriteTimeUtc } | Measure-Object -Maximum).Maximum; if ($maxSourceTime -gt $exeTime) { 'true' } else { 'false' } }"') do set "NEED_PUBLISH=%%I"

if /i "%NEED_PUBLISH%"=="true" (
    echo Publishing bootstrapper executable...
    dotnet publish "%BOOTSTRAPPER_PROJ%" -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
    if errorlevel 1 (
        echo ERROR: Failed to publish bootstrapper executable.
        pause
        exit /b 1
    )
) else (
    echo Bootstrapper executable is up to date. Skipping publish.
)

if /i "%MODE%"=="popup" (
    if not defined HECATEON_DISABLE_INPLACE_DASHBOARD set "HECATEON_DISABLE_INPLACE_DASHBOARD=true"
    "%BOOTSTRAPPER_EXE%"
    set "BOOT_EXIT=%ERRORLEVEL%"

    if "%BOOT_EXIT%"=="0" (
        start "HECATEON Status Matrix" cmd /k "echo ======================================== ^& echo HECATEON STATUS MATRIX ^(POST-LOAD^) ^& echo ======================================== ^& echo. ^& echo +----------------+----------+---------------------------------------+ ^& echo ^| Step           ^| Status   ^| Detail                                ^| ^& echo +----------------+----------+---------------------------------------+ ^& echo ^| Dependencies   ^| PASS     ^| Tools detected                        ^| ^& echo ^| Repository     ^| PASS     ^| Repository ready                      ^| ^& echo ^| Validation     ^| PASS/*   ^| Based on active profile               ^| ^& echo ^| Server         ^| PASS     ^| Server ready                          ^| ^& echo ^| AI Warmup      ^| PASS/*   ^| Best-effort or profile-based          ^| ^& echo ^| Publish        ^| PASS/*   ^| Based on active profile               ^| ^& echo ^| Launch         ^| PASS     ^| Desktop launched                      ^| ^& echo +----------------+----------+---------------------------------------+ ^& echo. ^& echo Bootstrap completed successfully. ^& echo ^(* PASS may appear as SKIPPED in offline/dev profiles^) ^& echo. ^& pause"
    )

    endlocal
    exit /b %BOOT_EXIT%
) else (
    if not defined HECATEON_DISABLE_INPLACE_DASHBOARD set "HECATEON_DISABLE_INPLACE_DASHBOARD=true"
    "%BOOTSTRAPPER_EXE%"
    set "BOOT_EXIT=%ERRORLEVEL%"
    endlocal
    exit /b %BOOT_EXIT%
)

:show_help
call :print_usage
endlocal
exit /b 0

:print_usage
echo Usage: RUN_LULLABY_DESKTOP_BATCH.cmd [main^|dev^|offline] [popup^|inline]
echo.
echo Profiles:
echo   main    - Use main branch, auto-update ON, validation ON, AI warmup ON
echo   dev     - Keep local branch, auto-update OFF, validation ON, AI warmup ON
echo   offline - Keep local branch, auto-update OFF, validation OFF, AI warmup OFF
echo.
echo Modes:
echo   popup   - Open post-load status popup on successful bootstrap ^(default^)
echo   inline  - Run in the current terminal
echo.
echo Help:
echo   RUN_LULLABY_DESKTOP_BATCH.cmd --help
echo   RUN_LULLABY_DESKTOP_BATCH.cmd -h
echo   RUN_LULLABY_DESKTOP_BATCH.cmd help
goto :eof
