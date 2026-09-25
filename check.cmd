@echo off
setlocal
cd /d "%~dp0"
dotnet run --project tests\QuestionBank.Checks\QuestionBank.Checks.csproj -c Release
if errorlevel 1 (
    pause
    exit /b 1
)
pause
