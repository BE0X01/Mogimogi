@echo off
setlocal
cd /d "%~dp0"
dotnet publish src\QuestionBank.WinForms\QuestionBank.WinForms.csproj -c Release -r win-x64 --self-contained true -o publish\win-x64
if errorlevel 1 (
    pause
    exit /b 1
)
echo Output: publish\win-x64\QuestionBank.WinForms.exe
echo Keep the entire output folder, including Data and DLL files.
pause
