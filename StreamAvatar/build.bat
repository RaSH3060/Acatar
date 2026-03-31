# Build script for StreamAvatar
@echo off
setlocal enabledelayedexpansion

echo ========================================
echo   StreamAvatar Build Script
echo ========================================
echo.

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to restore packages!
    pause
    exit /b 1
)

echo.
echo Building project...
dotnet build -c Release --no-restore
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Publishing application...
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
if %ERRORLEVEL% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build completed successfully!
echo   Output directory: .\publish
echo ========================================
pause
