@echo off
setlocal enabledelayedexpansion

:: ============================================================
::  CONFIGURATION — edit these two lines to fit your needs
:: ============================================================
set PACKAGE=<com.example.app>
set SAVE_PATH=<%USERPROFILE%\Path\To\Save\Files>
:: ============================================================

set REMOTE_PATH=/storage/emulated/0/Android/data/%PACKAGE%/files

echo.
echo ============================================================
echo   Quest ADB Tool
echo ============================================================

:: ── 1. ADB version ──────────────────────────────────────────
echo.
echo [1/4] Checking ADB version...
adb version > nul 2>&1
if %errorlevel% neq 0 (
    echo  ERROR: ADB not found. 
    echo  Install it via: winget install Google.PlatformTools
    echo  Or download from: https://developer.android.com/tools/releases/platform-tools
    pause
    exit /b 1
)
for /f "tokens=*" %%i in ('adb version') do (
    echo  %%i
    goto :adb_done
)
:adb_done

:: ── 2. Check device connected ───────────────────────────────
echo.
echo [2/4] Checking connected device...
for /f "skip=1 tokens=1,2" %%a in ('adb devices') do (
    if "%%b"=="device" (
        set DEVICE_ID=%%a
    )
)

if not defined DEVICE_ID (
    echo  ERROR: No device detected.
    echo  - Make sure the Quest is plugged in via USB
    echo  - Put the headset on and tap "Allow" on the USB debugging prompt
    pause
    exit /b 1
)

echo  Device ID : %DEVICE_ID%

:: ── 3. Device model ─────────────────────────────────────────
echo.
echo [3/4] Device info...
for /f "tokens=*" %%i in ('adb shell getprop ro.product.model') do set MODEL=%%i
for /f "tokens=*" %%i in ('adb shell getprop ro.build.version.release') do set ANDROID=%%i
for /f "tokens=*" %%i in ('adb shell getprop ro.product.manufacturer') do set BRAND=%%i

echo  Model     : %MODEL%
echo  Brand     : %BRAND%
echo  Android   : %ANDROID%
echo  Package   : %PACKAGE%

:: ── 4. Pull specific files ──────────────────────────────────
echo.
echo [4/4] Pulling selected files (.jpg, .png, .json)...
echo  From : %REMOTE_PATH%
echo  To   : %SAVE_PATH%
echo.

if not exist "%SAVE_PATH%" mkdir "%SAVE_PATH%"
set /a TOTAL_PULLED=0

for %%E in (jpg png json) do (
    echo  Checking for .%%E files...
    
    :: We removed the manual character stripping here
    for /f "tokens=*" %%F in ('adb shell "ls %REMOTE_PATH%/*.%%E 2>/dev/null"') do (
        set "REMOTE_FILE=%%F"
        
        :: The 'clean' way: we use the variable directly without chopping the end
        adb pull "!REMOTE_FILE!" "%SAVE_PATH%" > nul
        
        if !errorlevel! equ 0 (
            echo    [PULLED] %%~nxF
            set /a TOTAL_PULLED+=1
        )
    )
)

if %TOTAL_PULLED% gtr 0 (
    echo.
    echo  SUCCESS — %TOTAL_PULLED% files saved to:
    echo  %SAVE_PATH%
    echo.
    
    :: Recalculate counts for the summary
    for /f %%i in ('dir /b /a-d "%SAVE_PATH%\*.jpg" 2^>nul ^| find /c /v ""') do set JPG_COUNT=%%i
    for /f %%i in ('dir /b /a-d "%SAVE_PATH%\*.png" 2^>nul ^| find /c /v ""') do set PNG_COUNT=%%i
    for /f %%i in ('dir /b /a-d "%SAVE_PATH%\*.json" 2^>nul ^| find /c /v ""') do set JSON_COUNT=%%i
    
    echo  Summary in local folder:
    echo  JPG  files : !JPG_COUNT!
    echo  PNG  files : !PNG_COUNT!
    echo  JSON files : !JSON_COUNT!
    echo.
    explorer "%SAVE_PATH%"
) else (
    echo.
    echo  WARNING: No matching files found or folder is empty.
    echo  Check: %REMOTE_PATH%
)

echo ============================================================
pause
endlocal