MPV_VERSION=0.41.0
MPV_BUILDS_BASE_URL=https://github.com/jaysonsantos/mpv-builds/releases/download

MPV_TARGETS=windows-x86_64 android-x86_64 android-aarch64 android-armv7a macos-aarch64 ios-aarch64
MPV_TARGETS_DESTINATION=$(addprefix .cache/mpv/,$(MPV_TARGETS))
MPV_TARGETS_TAR=$(addsuffix .tar.gz,$(MPV_TARGETS_DESTINATION))

MOLTENVK_VERSION=1.4.1
MOLTENVK_URL=https://github.com/KhronosGroup/MoltenVK/releases/download/v$(MOLTENVK_VERSION)/MoltenVK-all.tar
MOLTENVK_TAR=.cache/MoltenVK-all.tar
MOLTENVK_DESTINATION=.cache/MoltenVK

Lib/Generated: jellyfin-openapi.json
	kiota generate -l CSharp -n JellyfinPlayer.Lib.Api -c JellyfinApiClient --exclude-backward-compatible -d $< -o ./Lib.JellyfinApi/Generated

jellyfin-openapi.json:
	curl -Lsqo $@ https://api.jellyfin.org/openapi/jellyfin-openapi-stable.json

$(MPV_TARGETS_TAR):
	@mkdir -p .cache/mpv
	@target=$(@:.cache/mpv/%.tar.gz=%); \
	url="$(MPV_BUILDS_BASE_URL)/$$target-mpv-$(MPV_VERSION)/$$target.tar.gz"; \
	echo "Downloading mpv for $$target from $$url"; \
	curl --fail -Lsqo $@ "$$url"


$(MPV_TARGETS_DESTINATION): $(MPV_TARGETS_TAR)
	@set -x; target=$(@:.cache/mpv/%=%); \
	echo "Extracting mpv for $$target"; \
	mkdir -p .cache/mpv/$$target; \
	tar -xvf .cache/mpv/$$target.tar.gz -C .cache/mpv

$(MOLTENVK_TAR):
	@mkdir -p .cache
	@echo "Downloading MoltenVK $(MOLTENVK_VERSION)"
	curl --fail -Lsqo $@ "$(MOLTENVK_URL)"

$(MOLTENVK_DESTINATION): $(MOLTENVK_TAR)
	@echo "Extracting MoltenVK"
	@mkdir -p $@
	@tar -xf $(MOLTENVK_TAR) -C $@

# Platform-specific dependency targets
ifeq ($(OS),Windows_NT)
deps: .cache/mpv/windows-x86_64
else
deps: $(MPV_TARGETS_DESTINATION) $(MOLTENVK_DESTINATION)
endif

# Test targets
.PHONY: test test-coverage test-coverage-html clean-test-results

test:
	dotnet test -f net10.0

test-coverage:
	dotnet test -f net10.0 --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults || true

test-coverage-html: test-coverage
	reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html
	@echo "Coverage report generated at TestResults/CoverageReport/index.html"
ifeq ($(OS),Windows_NT)
	start TestResults/CoverageReport/index.html
else
	open TestResults/CoverageReport/index.html 2>/dev/null || xdg-open TestResults/CoverageReport/index.html 2>/dev/null || echo "Open TestResults/CoverageReport/index.html in your browser"
endif
