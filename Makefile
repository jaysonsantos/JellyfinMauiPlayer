MPV_VERSION=0.41.0
MPV_BUILDS_BASE_URL=https://github.com/jaysonsantos/mpv-builds/releases/download

MPV_TARGETS=windows-x86_64 android-x86_64 android-aarch64 macos-aarch64 ios-aarch64
MPV_TARGETS_DESTINATION=$(addprefix .cache/mpv/,$(MPV_TARGETS))
MPV_TARGETS_TAR=$(addsuffix .tar.xz,$(MPV_TARGETS_DESTINATION))

MOLTENVK_VERSION=1.4.1
MOLTENVK_URL=https://github.com/KhronosGroup/MoltenVK/releases/download/v$(MOLTENVK_VERSION)/MoltenVK-all.tar
MOLTENVK_TAR=.cache/MoltenVK-all.tar
MOLTENVK_DESTINATION=.cache/MoltenVK

Lib/Generated: jellyfin-openapi.json
	kiota generate -l CSharp -n JellyfinPlayer.Lib.Api -c JellyfinApiClient --exclude-backward-compatible -d $< -o ./Lib/Generated

jellyfin-openapi.json:
	curl -Lsqo $@ https://api.jellyfin.org/openapi/jellyfin-openapi-stable.json

$(MPV_TARGETS_TAR):
	@mkdir -p .cache/mpv
	@target=$(@:.cache/mpv/%.tar.xz=%); \
	url="$(MPV_BUILDS_BASE_URL)/$$target-mpv-$(MPV_VERSION)/$$target-mpv-$(MPV_VERSION).tar.gz"; \
	echo "Downloading mpv for $$target from $$url"; \
	curl --fail -Lsqo $@ "$$url"


$(MPV_TARGETS_DESTINATION): $(MPV_TARGETS_TAR)
	@target=$(@:.cache/mpv/%=%); \
	echo "Extracting mpv for $$target"; \
	tar -xf .cache/mpv/$$target.tar.xz -C .cache/

$(MOLTENVK_TAR):
	@mkdir -p .cache
	@echo "Downloading MoltenVK $(MOLTENVK_VERSION)"
	curl --fail -Lsqo $@ "$(MOLTENVK_URL)"

$(MOLTENVK_DESTINATION): $(MOLTENVK_TAR)
	@echo "Extracting MoltenVK"
	@mkdir -p $@
	@tar -xf $(MOLTENVK_TAR) -C $@

deps: $(MPV_TARGETS_DESTINATION) $(MOLTENVK_DESTINATION)
