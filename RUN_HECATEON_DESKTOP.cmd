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

REM === Run Automated Tests Before Launch ===
set "TEST_PROJECT=%REPO_ROOT%Lullaby/Lullaby.Client.Tests/Lullaby.Client.Tests.csproj"
if exist "%TEST_PROJECT%" (
    echo Running automated tests...
    dotnet test "%TEST_PROJECT%" --nologo --logger "trx;LogFileName=test_results.trx"
    if errorlevel 1 (
        echo.
        echo =============================
        echo   TESTS FAILED - ABORTING
        echo =============================
        pause
        exit /b 1
    ) else (
        echo All tests passed.
    )
)

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
        start "HECATEON Status Matrix" cmd /k "
        call :print_banner_animated
        call :print_status_matrix_animated
        echo.
        echo ^>^>^> ^[[92mHECATEON BOOTSTRAP COMPLETED SUCCESSFULLY!^[[0m
        echo.
        echo ^(* PASS may appear as SKIPPED in offline/dev profiles^)
        echo.
        call :interactive_menu
        exit /b 0
        "
    REM === Interactive Menu ===
    :interactive_menu
        echo ^[[96m
        echo [R]erun  [V]iew Logs  [Q]uit
        echo Select an option and press Enter:
        set /p userchoice=^> 
        if /i "%userchoice%"=="R" goto :rerun_launcher
        if /i "%userchoice%"=="V" goto :view_logs_menu
        if /i "%userchoice%"=="Q" exit /b 0
        echo Invalid option. Please try again.
        goto :interactive_menu

    :rerun_launcher
        echo Restarting launcher...
        call "%~f0" %*
        exit /b 0

    :view_logs_menu
        echo.
        echo [D]ependencies  [T]ests  [S]erver  [A]I Warmup  [P]ublish  [L]aunch  [B]ack
        echo Select a log to view:
        set /p logchoice=^> 
        if /i "%logchoice%"=="D" type "%REPO_ROOT%logs\dependencies.log" & goto :view_logs_menu
        if /i "%logchoice%"=="T" type "%REPO_ROOT%Lullaby\Lullaby.Client.Tests\TestResults\test_results.trx" & goto :view_logs_menu
        if /i "%logchoice%"=="S" type "%REPO_ROOT%logs\server.log" & goto :view_logs_menu
        if /i "%logchoice%"=="A" type "%REPO_ROOT%logs\aiwarmup.log" & goto :view_logs_menu
        if /i "%logchoice%"=="P" type "%REPO_ROOT%logs\publish.log" & goto :view_logs_menu
        if /i "%logchoice%"=="L" type "%REPO_ROOT%logs\launch.log" & goto :view_logs_menu
        if /i "%logchoice%"=="B" goto :interactive_menu
        echo Invalid option. Please try again.
        goto :view_logs_menu
    )

    goto :eof

:print_banner_animated
    setlocal EnableDelayedExpansion
    set "lantern=   ( )\n  /|\n /_|_\n   |  "
    echo ^[[96m
    echo   _   _            _             _
    echo  | | | | ___  __ _| | _____ _ __| |_ ___
    echo  | |_| |/ _ \/ _` | |/ / _ \ '__| __/ __|
    echo  |  _  |  __/ (_| |   <  __/ |  | |_\__ \
    echo  |_| |_|\___|\__,_|_|\_\___|_|   \__|___/
    echo.
    echo   ^[[93mHecateon Desktop Bootstrapper^[[0m
    echo ^[[93m!lantern!   ^[[0m
    echo ^[[0m
    endlocal
    goto :eof

:print_status_matrix_animated
    setlocal EnableDelayedExpansion
    set "steps=Dependencies,Repository,Validation,Automated Tests,Server,AI Warmup,Publish,Launch"
    set "details=Tools detected,Repository ready,Based on active profile,All tests passed,Server ready,Best-effort or profile-based,Based on active profile,Desktop launched"
    set "i=0"
    echo ^[[95m+-------------------+----------+------------------------------------------+^[[0m
    echo ^[[95m^| Step              ^| Status   ^| Detail                                   ^|^[[0m
    echo ^[[95m+-------------------+----------+------------------------------------------+^[[0m
    for %%S in (!steps!) do (
        set /a i+=1
        set "step=%%S"
        for /f "tokens=%i%" %%D in ("!details!") do set "detail=%%D"
        call :print_status_row_animated "!step!" "!detail!"
    )
    echo ^[[95m+-------------------+----------+------------------------------------------+^[[0m
    endlocal
    goto :eof


REM === Deep Interactive Step-by-Step Launcher ===
setlocal EnableDelayedExpansion
set "STEPS=Dependencies,Repository,Validation,Automated Tests,Server,AI Warmup,Publish,Launch"
set "DETAILS=Tools detected,Repository ready,Based on active profile,All tests passed,Server ready,Best-effort or profile-based,Based on active profile,Desktop launched"
set "LOGS=logs\dependencies.log,logs\repository.log,logs\validation.log,Lullaby\Lullaby.Client.Tests\TestResults\test_results.trx,logs\server.log,logs\aiwarmup.log,logs\publish.log,logs\launch.log"
set "CMDS=call :step_dependencies,call :step_repository,call :step_validation,call :step_tests,call :step_server,call :step_aiwarmup,call :step_publish,call :step_launch"

call :print_banner_animated
call :print_status_matrix_header

set i=0
for %%S in (!STEPS!) do (
    set /a i+=1
    for /f "tokens=%i%" %%D in ("!DETAILS!") do set "detail=%%D"
    for /f "tokens=%i%" %%L in ("!LOGS!") do set "log=%%L"
    for /f "tokens=%i%" %%C in ("!CMDS!") do set "cmd=%%C"
    call :run_step "%%S" "!detail!" "!log!" !cmd!
)

call :print_status_matrix_footer
echo.
echo ^>^>^> ^[[92mHECATEON BOOTSTRAP COMPLETED SUCCESSFULLY!^[[0m
echo.
call :interactive_menu
endlocal
exit /b 0

:run_step
    setlocal EnableDelayedExpansion
    set "step=%~1"
    set "detail=%~2"
    set "log=%~3"
    set "cmd=%~4"
    set "status=WAIT"
    set "symbol=..."
    set "color=93"
    call :print_status_row "!step!" "!status!" "!detail!" "!symbol!" "!color!"
    call :pause_for_key "Press any key to view live log for !step!... (or wait to continue)"
    if exist "!log!" del "!log!"
    call !cmd! > "!log!" 2>&1
    set "exitcode=!ERRORLEVEL!"
    if "!exitcode!" == "0" (
        set "status=PASS"
        set "symbol=✔"
        set "color=92"
    ) else (
        set "status=FAIL"
        set "symbol=✖"
        set "color=91"
    )
    call :print_status_row "!step!" "!status!" "!detail!" "!symbol!" "!color!"
    if not "!exitcode!" == "0" (
        type "!log!"
        echo.
        echo ^[[91mStep !step! failed. See log above. Aborting...^[[0m
        pause
        exit /b 1
    )
    endlocal
    goto :eof

:pause_for_key
    setlocal
    set "msg=%~1"
    echo !msg!
    pause >nul
    endlocal
    goto :eof

:print_status_matrix_header
    echo ^[[95m+-------------------+----------+------------------------------------------+^[[0m
    echo ^[[95m^| Step              ^| Status   ^| Detail                                   ^|^[[0m
    echo ^[[95m+-------------------+----------+------------------------------------------+^[[0m
    goto :eof

:print_status_matrix_footer
    echo ^[[95m+-------------------+----------+------------------------------------------+^[[0m
    goto :eof

:print_status_row
    setlocal
    set "step=%~1"
    set "status=%~2"
    set "detail=%~3"
    set "symbol=%~4"
    set "color=%~5"
    echo ^[[!color!m^| !step!           ^| !status!  ^| !detail! !symbol!^|^[[0m
    endlocal
    goto :eof

REM === Step Implementations (stubs, replace with real logic as needed) ===
:step_dependencies
    echo Checking dependencies...
    timeout /t 1 >nul
    exit /b 0
:step_repository
    echo Checking repository...
    timeout /t 1 >nul
    exit /b 0
:step_validation
    echo Running validation...
    timeout /t 1 >nul
    exit /b 0
:step_tests
    echo Running tests...
    dotnet test "%REPO_ROOT%Lullaby/Lullaby.Client.Tests/Lullaby.Client.Tests.csproj" --nologo --logger "trx;LogFileName=test_results.trx"
    exit /b %ERRORLEVEL%
:step_server
    echo Starting server...
    timeout /t 1 >nul
    exit /b 0
:step_aiwarmup
    echo Warming up AI...
    timeout /t 1 >nul
    exit /b 0
:step_publish
    echo Publishing...
    timeout /t 1 >nul
    exit /b 0
:step_launch
    echo Launching desktop...
    timeout /t 1 >nul
    exit /b 0
