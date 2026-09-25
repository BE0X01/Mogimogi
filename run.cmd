@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
    echo Install .NET 10 SDK for Windows, then run this file again.
    echo https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)
dotnet run --project src\QuestionBank.WinForms\QuestionBank.WinForms.csproj -c Release
if errorlevel 1 pause
