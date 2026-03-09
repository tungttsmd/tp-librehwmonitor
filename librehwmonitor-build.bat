@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
cls
set "ROOT=%~dp0"
set "BUILD_DIR=%ROOT%libreHwMonitor"
set "DEPLOY_DIR=%ROOT%tp-net-librehwmonitor"
for /f "tokens=*" %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"
set "CYAN=%ESC%[36m"
set "GREEN=%ESC%[32m"
set "RED=%ESC%[31m"
set "RESET=%ESC%[0m"

echo %CYAN%[INFO]%RESET% [1/5] Kiem tra .NET SDK...
where dotnet >nul 2>&1
if !errorlevel! neq 0 (
    echo %RED%[ERROR]%RESET% Khong tim thay dotnet SDK.
    pause & exit /b 1
)
dotnet --version > "%TEMP%\dotver.tmp" 2>&1
set /p DOTVER=<"%TEMP%\dotver.tmp"
del "%TEMP%\dotver.tmp" >nul 2>&1
for /f "tokens=1 delims=." %%v in ("!DOTVER!") do set MAJOR=%%v
if !MAJOR! LSS 8 (
    echo %RED%[ERROR]%RESET% Yeu cau .NET SDK ^>= 8.0. Hien tai: !DOTVER!
    pause & exit /b 1
)
echo %GREEN%[OK]%RESET% .NET SDK !DOTVER!

echo %CYAN%[INFO]%RESET% [2/5] Chuan bi files...
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"
for %%f in (EnvLoader.cs Program.cs DataRaw.cs DataCleaner.cs MetricWebserver.cs libreHwMonitor.csproj) do (
    copy "%ROOT%%%f" "%BUILD_DIR%\" >nul
)
cd /d "%BUILD_DIR%"

echo %CYAN%[INFO]%RESET% [3/5] Build...
dotnet restore >nul && dotnet build --configuration Release --no-restore >nul
if !errorlevel! neq 0 (
    echo %RED%[ERROR]%RESET% Build that bai.
    pause & exit /b 1
)

echo %CYAN%[INFO]%RESET% [4/5] Deploy...
if exist "%DEPLOY_DIR%" rmdir /s /q "%DEPLOY_DIR%"
xcopy "%BUILD_DIR%\bin\Release\net462" "%DEPLOY_DIR%\" /e /i /q

echo.
echo %CYAN%[INFO]%RESET% [5/5] Moving cac file bin vao deploy folder...
copy "%ROOT%bin\net-462-install.bat" "%DEPLOY_DIR%\" >nul
copy "%ROOT%bin\run.bat" "%DEPLOY_DIR%\" >nul
cd /d "%ROOT%"
rmdir /s /q "%BUILD_DIR%"
echo %GREEN%[DONE]%RESET% Trien khai thanh cong tai: tp-net-librehwmonitor\
pause
endlocal