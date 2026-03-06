@echo off
chcp 65001 >nul
cls

set "ROOT=%~dp0"
set "BUILD_DIR=%ROOT%libreHwMonitor"
set "DEPLOY_DIR=%ROOT%tp-net-librehwmonitor"

:: Enable ANSI colors (Windows 10+)
for /f "tokens=*" %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"
set "CYAN=%ESC%[36m"
set "GREEN=%ESC%[32m"
set "RED=%ESC%[31m"
set "RESET=%ESC%[0m"

echo %CYAN%[INFO]%RESET% [1/4] Kiem tra .NET SDK...
where dotnet >nul 2>&1 || (echo %RED%[ERROR]%RESET% Khong tim thay dotnet SDK. & pause & exit /b 1)
for /f "tokens=1 delims=." %%v in ('dotnet --version') do set MAJOR=%%v
if %MAJOR% LSS 8 (echo %RED%[ERROR]%RESET% Yeu cau .NET SDK >= 8.0. & pause & exit /b 1)

echo %CYAN%[INFO]%RESET% [2/4] Chuan bi files...
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"
for %%f in (EnvLoader.cs Program.cs DataRaw.cs DataCleaner.cs MetricWebserver.cs libreHwMonitor.csproj) do (
    copy "%ROOT%%%f" "%BUILD_DIR%\" >nul
)
cd /d "%BUILD_DIR%"

echo %CYAN%[INFO]%RESET% [3/4] Build...
dotnet restore >nul && dotnet build --configuration Release --no-restore >nul
if %ERRORLEVEL% neq 0 (echo %RED%[ERROR]%RESET% Build that bai. & pause & exit /b 1)

echo %CYAN%[INFO]%RESET% [4/4] Deploy...
if exist "%DEPLOY_DIR%" rmdir /s /q "%DEPLOY_DIR%"
xcopy "%BUILD_DIR%\bin\Release\net462" "%DEPLOY_DIR%\" /e /i /q
copy "%ROOT%batFile\run.bat" "%DEPLOY_DIR%\" >nul
cd /d "%ROOT%"
rmdir /s /q "%BUILD_DIR%"

echo %GREEN%[DONE]%RESET% Trien khai thanh cong tai: tp-net-librehwmonitor\
pause