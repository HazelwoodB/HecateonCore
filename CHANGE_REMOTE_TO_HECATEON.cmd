@echo off
REM Change Git Remote to HecateonCore Repository

echo.
echo ================================================
echo  Changing Git Remote to HecateonCore
echo ================================================
echo.

cd /d "%USERPROFILE%\source\repos\Lullaby"

REM Find Git installation
set "GIT_CMD="
if exist "C:\Program Files\Git\cmd\git.exe" set "GIT_CMD=C:\Program Files\Git\cmd\git.exe"
if exist "C:\Program Files (x86)\Git\cmd\git.exe" set "GIT_CMD=C:\Program Files (x86)\Git\cmd\git.exe"
if exist "%LOCALAPPDATA%\Programs\Git\cmd\git.exe" set "GIT_CMD=%LOCALAPPDATA%\Programs\Git\cmd\git.exe"

if "%GIT_CMD%"=="" (
    echo ERROR: Git not found. Please install Git or run this from Git Bash.
    pause
    exit /b 1
)

echo Current remote:
"%GIT_CMD%" remote -v
echo.

echo Changing remote URL...
"%GIT_CMD%" remote set-url origin https://github.com/HazelwoodB/HecateonCore.git

echo.
echo New remote:
"%GIT_CMD%" remote -v

echo.
echo ================================================
echo  Remote successfully changed!
echo ================================================
echo.
pause
