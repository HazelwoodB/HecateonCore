@echo off
setlocal
echo [DEPRECATED] RUN_LULLABY_DESKTOP_BATCH.cmd is deprecated.
echo              Using single start entrypoint: START.cmd
call "%~dp0START.cmd" %*
set "EXIT_CODE=%ERRORLEVEL%"
endlocal
exit /b %EXIT_CODE%