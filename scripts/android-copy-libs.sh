#!/bin/bash

set -eo pipefail

NDK_BIN="$(dirname "$(jq -r '.host.cpp.exelist[0]' meson-info/intro-compilers.json)")"

# Detect architecture from the compiler path
if echo "$NDK_BIN" | grep -q "aarch64"; then
    ARCH="aarch64"
    ARCH_DIR="aarch64-linux-android"
elif echo "$NDK_BIN" | grep -q "x86_64"; then
    ARCH="x86_64"
    ARCH_DIR="x86_64-linux-android"
else
    echo "Unknown architecture in compiler path: $NDK_BIN"
    exit 1
fi

mkdir -p "../prefix/android/$ARCH/lib"
cp "$NDK_BIN/../sysroot/usr/lib/$ARCH_DIR/libc++_shared.so" "../prefix/android/$ARCH/lib/"
