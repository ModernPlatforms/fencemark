@echo off
REM Quick launcher for E2E tests
REM Double-click this file to run tests with default settings

echo.
echo ================================================
echo Fencemark E2E Test Quick Launcher
echo ================================================
echo.

REM Change to the Tests directory
cd /d "%~dp0.."

REM Run the PowerShell script
powershell -ExecutionPolicy Bypass -File "E2E\Run-E2ETests.ps1"

echo.
pause
