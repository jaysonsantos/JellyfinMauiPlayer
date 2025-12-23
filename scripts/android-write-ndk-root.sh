#!/bin/bash
set -eo pipefail
NDK_ROOT="$(grep --color=auto -oE '/.+?clang' "$1" | head -n 1 | sed 's|/toolchains/.*||')"
if grep CMAKE_ANDROID_NDK "$1"; then
   exit 0
fi

printf "\n[cmake]\nCMAKE_ANDROID_NDK = '%s'\nNDK_PROC_aarch64_ABI='arm64-v8a'\nNDK_PROC_x86_64_ABI='x86_64'\nCMAKE_POSITION_INDEPENDENT_CODE='ON'\n" "$NDK_ROOT" >> "$1"
