#!/bin/bash

set -eo pipefail

NDK_BIN="$(dirname "$(jq -r '.host.cpp.exelist[0]' meson-info/intro-compilers.json)")"
cp "$NDK_BIN/../sysroot/usr/lib/aarch64-linux-android/libc++_shared.so" ../prefix/android/lib/
