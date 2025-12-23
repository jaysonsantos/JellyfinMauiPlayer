MPV_VERSION=v0.41.0
MPV_LINK=https://github.com/mpv-player/mpv/archive/refs/tags/$(MPV_VERSION).tar.gz
MPV_TAR=.cache/mpv-$(MPV_VERSION).tar.gz

WRAPS=expat harfbuzz libpng zlib mbedtls
WRAP_FILES=$(patsubst %,.cache/mpv/subprojects/%.wrap,$(WRAPS))
GSTREAMER_WRAPS=freetype2 fribidi fontconfig libjpeg-turbo
GSTREAMER_WRAP_FILES=$(patsubst %,.cache/mpv/subprojects/%.wrap,$(GSTREAMER_WRAPS))
GSTREAMER_WRAP_URL=https://gitlab.freedesktop.org/gstreamer/gstreamer/-/raw/main/subprojects
LOCAL_WRAPS=$(wildcard wraps/*.wrap)
LOCAL_WRAP_FILES=$(patsubst wraps/%.wrap,.cache/mpv/subprojects/%.wrap,$(LOCAL_WRAPS))

ARCH = aarch64
CROSS_FILE = .cache/mpv/androidcross/android-27.0.12077973-android21-$(ARCH)-cross.txt
ANDROID_NDK := $(shell grep --color=auto -oE '/.+?clang' $(CROSS_FILE) | head -n 1 | sed 's|/toolchains/.*||' )

Lib/Generated: jellyfin-openapi.json
	kiota generate -l CSharp -n JellyfinPlayer.Lib.Api -c JellyfinApiClient --exclude-backward-compatible -d $< -o ./Lib/Generated

jellyfin-openapi.json:
	curl -Lsqo $@ https://api.jellyfin.org/openapi/jellyfin-openapi-stable.json

.cache/mpv: $(MPV_TAR)
	mkdir -p .cache/
	$(eval MPV_DIR := $(shell tar -tf $(MPV_TAR) | head -n 1))
	tar -xf $(MPV_TAR) -C .cache/
	mv .cache/$(MPV_DIR) .cache/mpv

$(MPV_TAR):
	mkdir -p .cache/
	curl -Lsqo $@ $(MPV_LINK)

$(WRAP_FILES): .cache/mpv
	@mkdir -p .cache/mpv/subprojects
	@test -f $@ || (cd .cache/mpv && meson wrap install $(basename $(notdir $@)))

$(GSTREAMER_WRAP_FILES): .cache/mpv
	@mkdir -p .cache/mpv/subprojects
	@test -f $@ || curl -Lsqo $@ $(GSTREAMER_WRAP_URL)/$(notdir $@)

$(LOCAL_WRAP_FILES): .cache/mpv/subprojects/%.wrap: wraps/%.wrap
	@mkdir -p .cache/mpv/subprojects
	cp $< $@

wraps: $(WRAP_FILES) $(GSTREAMER_WRAP_FILES) $(LOCAL_WRAP_FILES)

$(CROSS_FILE): .cache/mpv
	cd .cache/mpv && meson env2mfile --android -o androidcross
	./scripts/android-write-ndk-root.sh $(CROSS_FILE)

.cache/mpv/subprojects/FFmpeg/.jni-patched: wraps patches/ffmpeg-jni-fix.patch
	@echo "Applying FFmpeg JNI fix patch..."
	@cd .cache/mpv/subprojects/FFmpeg && patch -p1 < ../../../../patches/ffmpeg-jni-fix.patch
	@touch $@

android-compile: $(CROSS_FILE) .cache/mpv/subprojects/FFmpeg/.jni-patched
	cd .cache/mpv && \
	meson setup buildAndroid-$(ARCH) --default-library=shared --buildtype=release \
		-Dwrap_mode=forcefallback -Dlibmpv=true -Dgpl=true -Dshaderc=disabled -Dharfbuzz:icu=disabled \
		-Dlibass:require-system-font-provider=false \
		$(if $(filter x86_64,$(ARCH)),-Dlibass:asm=disabled -DFFmpeg:x86asm=disabled) \
		-DFFmpeg:gpl=enabled \
		-DFFmpeg:version3=enabled \
		-DFFmpeg:mbedtls=enabled \
		-DFFmpeg:tls_protocol=enabled \
		-DFFmpeg:jni=enabled \
		-DFFmpeg:pthreads=enabled \
		--cross-file ../../$(CROSS_FILE) \
		--prefix=$(abspath .cache)/prefix/android/$(ARCH) || true && \
	cd buildAndroid-$(ARCH) && \
	ninja install && \
	../../../scripts/android-copy-libs.sh
