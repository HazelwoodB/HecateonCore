@echo off
setlocal
echo [DEPRECATED] RUN_LULLABY_DESKTOP_BATCH.cmd is deprecated and will be removed after two minor versions.
echo             Use RUN_HECATEON_DESKTOP.cmd instead.
call "%~dp0RUN_HECATEON_DESKTOP.cmd" %*
set "EXIT_CODE=%ERRORLEVEL%"
endlocal
exit /b %EXIT_CODE%
