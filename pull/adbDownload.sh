#!/usr/bin/env bash
set -uo pipefail

# ============================================================
#  CONFIGURATION — edit these two lines to fit your needs
# ============================================================
PACKAGE="com.example.app"
SAVE_PATH="$HOME/Path/To/Save/Files"
# ============================================================

REMOTE_PATH="/storage/emulated/0/Android/data/${PACKAGE}/files"

echo
echo "============================================================"
echo "  Quest ADB Tool"
echo "============================================================"

# ── 1. ADB version ──────────────────────────────────────────
echo
echo "[1/4] Checking ADB version..."
if ! command -v adb >/dev/null 2>&1; then
    echo " ERROR: ADB not found."
    echo " Install it via: sudo apt install android-tools-adb"
    echo " Or download from: https://developer.android.com/tools/releases/platform-tools"
    exit 1
fi
adb version | head -n 1 | sed 's/^/ /'

# ── 2. Check device connected ───────────────────────────────
echo
echo "[2/4] Checking connected device..."
DEVICE_ID=$(adb devices | awk 'NR>1 && $2=="device" {print $1; exit}')

if [ -z "${DEVICE_ID:-}" ]; then
    echo " ERROR: No device detected."
    echo " - Make sure the Quest is plugged in via USB"
    echo " - Put the headset on and tap \"Allow\" on the USB debugging prompt"
    exit 1
fi

echo " Device ID : $DEVICE_ID"

# ── 3. Device model ─────────────────────────────────────────
echo
echo "[3/4] Device info..."
MODEL=$(adb shell getprop ro.product.model | tr -d '\r')
ANDROID=$(adb shell getprop ro.build.version.release | tr -d '\r')
BRAND=$(adb shell getprop ro.product.manufacturer | tr -d '\r')

echo " Model     : $MODEL"
echo " Brand     : $BRAND"
echo " Android   : $ANDROID"
echo " Package   : $PACKAGE"

# ── 4. Pull specific files ──────────────────────────────────
echo
echo "[4/4] Pulling selected files (.jpg, .png, .json)..."
echo " From : $REMOTE_PATH"
echo " To   : $SAVE_PATH"
echo

mkdir -p "$SAVE_PATH"
TOTAL_PULLED=0

for EXT in jpg png json; do
    echo " Checking for .$EXT files..."

    # List matching remote files (strip any trailing \r from adb shell output)
    REMOTE_FILES=$(adb shell "ls ${REMOTE_PATH}/*.${EXT} 2>/dev/null" | tr -d '\r')

    if [ -z "$REMOTE_FILES" ]; then
        continue
    fi

    while IFS= read -r REMOTE_FILE; do
        [ -z "$REMOTE_FILE" ] && continue
        if adb pull "$REMOTE_FILE" "$SAVE_PATH" >/dev/null 2>&1; then
            echo "   [PULLED] $(basename "$REMOTE_FILE")"
            TOTAL_PULLED=$((TOTAL_PULLED + 1))
        fi
    done <<< "$REMOTE_FILES"
done

if [ "$TOTAL_PULLED" -gt 0 ]; then
    echo
    echo " SUCCESS — $TOTAL_PULLED files saved to:"
    echo " $SAVE_PATH"
    echo

    JPG_COUNT=$(find "$SAVE_PATH" -maxdepth 1 -type f -name '*.jpg' | wc -l)
    PNG_COUNT=$(find "$SAVE_PATH" -maxdepth 1 -type f -name '*.png' | wc -l)
    JSON_COUNT=$(find "$SAVE_PATH" -maxdepth 1 -type f -name '*.json' | wc -l)

    echo " Summary in local folder:"
    echo " JPG  files : $JPG_COUNT"
    echo " PNG  files : $PNG_COUNT"
    echo " JSON files : $JSON_COUNT"
    echo

    # Try to open the folder if a graphical file manager is available
    if command -v xdg-open >/dev/null 2>&1; then
        xdg-open "$SAVE_PATH" >/dev/null 2>&1 &
    fi
else
    echo
    echo " WARNING: No matching files found or folder is empty."
    echo " Check: $REMOTE_PATH"
fi

echo "============================================================"