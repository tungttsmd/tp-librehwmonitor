@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
cls
for /f "tokens=*" %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"
set "CYAN=%ESC%[36m"
set "GREEN=%ESC%[32m"
set "RED=%ESC%[31m"
set "YELLOW=%ESC%[33m"
set "RESET=%ESC%[0m"
:: Auto request admin
net session >nul 2>&1
if !errorlevel! neq 0 (
    echo  Requesting admin privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)
echo.
echo %CYAN%  Install .NET Framework 4.6.2%RESET%
echo  -------------------------------------------------------------------------------
echo.
:: ============================================================
:: CHECK IF ALREADY INSTALLED
:: ============================================================
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >nul 2>&1
if !errorlevel! == 0 (
    for /f "tokens=3" %%a in ('reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2^>nul') do set "RELEASE=%%a"
    if !RELEASE! geq 394802 (
        echo  %GREEN%[OK]%RESET%    .NET Framework 4.6.2 or higher already installed.
        echo.
        pause
        exit /b 0
    )
)
echo  %YELLOW%[WARN]%RESET%  .NET Framework 4.6.2 not found. Downloading...
echo.
:: ============================================================
:: DOWNLOAD
:: ============================================================
set "URL=https://go.microsoft.com/fwlink/?linkid=780600"
set "OUT=%TEMP%\ndp462-kb3151800-x86-x64-allos-enu.exe"
powershell -NoProfile -ExecutionPolicy Bypass -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor 3072; (New-Object System.Net.WebClient).DownloadFile('%URL%', '%OUT%')"
if !errorlevel! neq 0 (
    echo.
    echo  %RED%[ERROR]%RESET% Download failed.
    echo.
    pause
    exit /b 1
)
echo  %GREEN%[OK]%RESET%    Download complete.
echo.
:: ============================================================
:: INSTALL
:: ============================================================
echo  %CYAN%[INFO]%RESET%  Installing .NET Framework 4.6.2...
echo.
"%OUT%" /quiet /norestart
if !errorlevel! neq 0 (
    if !errorlevel! == 3010 (
        echo  %YELLOW%[WARN]%RESET%  Install complete. Restart required.
    ) else (
        echo.
        echo  %RED%[ERROR]%RESET% Install failed. Exit code: !errorlevel!
        echo.
        pause
        exit /b 1
    )
) else (
    echo  %GREEN%[OK]%RESET%    .NET Framework 4.6.2 installed successfully.
)
del "%OUT%" >nul 2>&1
echo.
echo  -------------------------------------------------------------------------------
echo.
pause
endlocal