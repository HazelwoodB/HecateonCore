@echo off
setlocal
echo [DEPRECATED] START_LULLABY_DESKTOP.cmd is deprecated. Use START_HECATEON_DESKTOP.cmd.
call "%~dp0START_HECATEON_DESKTOP.cmd" %*
set "EXIT_CODE=%ERRORLEVEL%"
endlocal
exit /b %EXIT_CODE%

