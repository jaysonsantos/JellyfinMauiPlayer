#!/usr/bin/env bash
set -e

# Script to fix rpath and setup Vulkan for MPV native libraries
# Note: MSBuild already copies MPV libraries via PostBuild target in .csproj
# This script only handles post-copy configuration

# Determine the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Determine build configuration (default to Debug)
CONFIGURATION="${1:-Debug}"

# Determine target framework (default to net10.0)
FRAMEWORK="${2:-net10.0}"

# Test output directory
OUTPUT_DIR="$SCRIPT_DIR/bin/$CONFIGURATION/$FRAMEWORK"

echo "=== MPV Native Libraries Setup ==="
echo "Configuration: $CONFIGURATION"
echo "Framework: $FRAMEWORK"
echo "Output Directory: $OUTPUT_DIR"
echo ""

# Check if output directory exists
if [ ! -d "$OUTPUT_DIR" ]; then
    echo "ERROR: Output directory not found: $OUTPUT_DIR"
    echo "Run 'dotnet build Mpv.Sys.Tests/Mpv.Sys.Tests.csproj -f $FRAMEWORK' first"
    exit 1
fi

# Detect OS
OS="$(uname -s)"
case "$OS" in
    Darwin*)
        PLATFORM="macos"
        ;;
    Linux*)
        PLATFORM="linux"
        echo "Linux detected: Using system libmpv (no additional setup needed)"
        exit 0
        ;;
    MINGW*|MSYS*|CYGWIN*)
        PLATFORM="windows"
        echo "Windows detected: No additional setup needed"
        exit 0
        ;;
    *)
        echo "Unsupported OS: $OS"
        exit 1
        ;;
esac

echo "Platform: $PLATFORM"
echo ""

# macOS-specific setup
if [ "$PLATFORM" = "macos" ]; then
    # Find all dylib files in output directory
    for dylib in "$OUTPUT_DIR"/*.dylib; do
        if [ -f "$dylib" ]; then
            echo "  Processing: $(basename "$dylib")"
            # Add @loader_path to rpath so libraries can find each other
            install_name_tool -add_rpath "@loader_path" "$dylib" 2>/dev/null || true
        fi
    done

    echo ""
    echo "✓ Successfully configured MPV libraries for macOS"
    echo "  Libraries are in: $OUTPUT_DIR"
fi

echo ""
echo "=== Setup Complete ==="
echo "You can now run tests with:"
echo "  dotnet test Mpv.Sys.Tests/Mpv.Sys.Tests.csproj -f $FRAMEWORK"
