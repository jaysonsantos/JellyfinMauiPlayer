# FFmpeg Patches

## ffmpeg-jni-fix.patch

**Issue**: FFmpeg's meson build for Android had JNI support disabled even when `-DFFmpeg:jni=enabled` was passed. The `av_jni_set_java_vm` and `av_jni_get_java_vm` functions were compiled as stubs returning `-ENOSYS` (-38).

**Root Cause**: The FFmpeg meson.build file checked for the `jni.h` header but never actually set `conf.set10('jni', true)` when both the header and pthreads were available. The logic only had an error check with an inverted condition.

**Fix**: Updated lines 2319-2323 of `meson.build` to:
1. Properly enable JNI when both `jni.h` and pthreads are available
2. Provide clear error messages when requirements are missing
3. Set `CONFIG_JNI=1` in FFmpeg's config.h

**Verification**: After applying the patch, you can verify JNI is enabled by checking:
```bash
objdump -S --disassemble-symbols=av_jni_set_java_vm --disassemble .cache/mpv/prefix/android/lib/libavcodec.so
```

The function should have a full implementation, not just `mov w0, #-0x26` (return -38).

**Application**: This patch is automatically applied during the build process via the Makefile. The patch target `.cache/mpv/subprojects/FFmpeg/.jni-patched` ensures it's only applied once.
