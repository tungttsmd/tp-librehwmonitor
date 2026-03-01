@echo off
cd /d "%~dp0"

"%~dp0libreHwMonitor.exe"

exit /b %ERRORLEVEL%