#!/usr/bin/env bash
set -e

# Script to fix rpath and setup Vulkan for MPV native libraries
# Note: MSBuild already copies MPV libraries via PostBuild target in .csproj
# This script only handles post-copy configuration
#
# Optimization: The script caches processed files to avoid redundant install_name_tool operations.
# Cache files are stored in .cache/rpath-processed/ and contain the modification timestamp
# of each processed dylib. Files are only reprocessed if they are newer than the cache.

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
    # Create cache directory for tracking processed files
    CACHE_DIR="$SCRIPT_DIR/.cache/rpath-processed"
    mkdir -p "$CACHE_DIR"
    
    # Find all dylib files in output directory
    processed_count=0
    skipped_count=0
    
    for dylib in "$OUTPUT_DIR"/*.dylib; do
        if [ -f "$dylib" ]; then
            dylib_name="$(basename "$dylib")"
            cache_file="$CACHE_DIR/$dylib_name.timestamp"
            
            # Get current file modification time (using macOS/BSD stat syntax)
            dylib_mtime=$(stat -f "%m" "$dylib" 2>/dev/null || echo "0")
            
            # Check if we need to process this file
            should_process=false
            
            if [ ! -f "$cache_file" ]; then
                # No cache file exists, need to process
                should_process=true
            else
                # Compare modification times
                cache_mtime=$(cat "$cache_file" 2>/dev/null || echo "0")
                
                if [ "$dylib_mtime" -gt "$cache_mtime" ]; then
                    # File is newer than cache, need to process
                    should_process=true
                fi
            fi
            
            if [ "$should_process" = true ]; then
                echo "  Processing: $dylib_name"
                # Add @loader_path to rpath so libraries can find each other
                install_name_tool -add_rpath "@loader_path" "$dylib" 2>/dev/null || true
                # Update cache with current modification time (regardless of success/failure)
                echo "$dylib_mtime" > "$cache_file"
                processed_count=$((processed_count + 1))
            else
                skipped_count=$((skipped_count + 1))
            fi
        fi
    done

    echo ""
    if [ $processed_count -gt 0 ]; then
        echo "✓ Processed $processed_count dylib file(s)"
    fi
    if [ $skipped_count -gt 0 ]; then
        echo "✓ Skipped $skipped_count unchanged dylib file(s)"
    fi
    echo "✓ Successfully configured MPV libraries for macOS"
    echo "  Libraries are in: $OUTPUT_DIR"
fi

echo ""
echo "=== Setup Complete ==="
echo "You can now run tests with:"
echo "  dotnet test Mpv.Sys.Tests/Mpv.Sys.Tests.csproj -f $FRAMEWORK"
