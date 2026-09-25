@echo off
setlocal
cd /d "%~dp0"
dotnet run --project tests\Mogimogi.Checks\Mogimogi.Checks.csproj -c Release
if errorlevel 1 (
    pause
    exit /b 1
)
pause
