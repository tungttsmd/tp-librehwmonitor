@echo off
cd /d "%~dp0"

:: Check .NET 4.6.2
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >nul 2>&1
if %errorlevel% neq 0 goto install_net
for /f "tokens=3" %%a in ('reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release') do set RELEASE=%%a
if %RELEASE% geq 394802 goto run

:install_net
echo .NET Framework 4.6.2 not found. Installing...
call "%~dp0net-462-install.bat"

:run
"%~dp0libreHwMonitor.exe"
exit /b %ERRORLEVEL%