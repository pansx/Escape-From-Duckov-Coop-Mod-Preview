@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo Escape From Duckov Coop Mod - Build Script
echo ========================================
echo.

:: Set default configuration
set "BUILD_CONFIGURATION=Release"
if "%1"=="Debug" set "BUILD_CONFIGURATION=Debug"

:: Clean old build output
echo [INFO] Cleaning old build output...
if exist "EscapeFromDuckovCoopMod\bin" rmdir /s /q "EscapeFromDuckovCoopMod\bin"
if exist "EscapeFromDuckovCoopMod\obj" rmdir /s /q "EscapeFromDuckovCoopMod\obj"

:: Start building
echo [INFO] Starting build...
echo Configuration: %BUILD_CONFIGURATION%
echo.

dotnet build EscapeFromDuckovCoopMod.sln --configuration %BUILD_CONFIGURATION% --verbosity minimal

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build failed!
    echo Please check the error messages above
    exit /b 1
)

:: Build successful
echo.
echo [SUCCESS] Build completed!

:: Copy localization files
echo [INFO] Copying localization files...
set "OUTPUT_DIR=EscapeFromDuckovCoopMod\bin\%BUILD_CONFIGURATION%\netstandard2.1"
set "LOCALIZATION_OUTPUT_DIR=%OUTPUT_DIR%\Localization"

if exist "Localization" (
    if not exist "%LOCALIZATION_OUTPUT_DIR%" mkdir "%LOCALIZATION_OUTPUT_DIR%"
    copy "Localization\*.json" "%LOCALIZATION_OUTPUT_DIR%\" /Y >nul
    if !ERRORLEVEL! equ 0 (
        echo [SUCCESS] Localization files copied
    ) else (
        echo [WARNING] Failed to copy localization files
    )
) else (
    echo [WARNING] Localization directory not found
)

:: Display output file info
if exist "%OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll" (
    echo [SUCCESS] Output file: %OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll
    
    :: Get file size
    for %%F in ("%OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll") do (
        set "filesize=%%~zF"
        set /a "filesizeKB=!filesize!/1024"
        echo [INFO] File size: !filesizeKB! KB
    )
) else (
    echo [ERROR] Output file not found: %OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll
)

echo.
echo Build completed successfully!