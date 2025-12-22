Lib/Generated: jellyfin-openapi.json
	kiota generate -l CSharp -n JellyfinPlayer.Lib.Api -c JellyfinApiClient --exclude-backward-compatible -d $< -o ./Lib/Generated

jellyfin-openapi.json:
	curl -Lsqo $@ https://api.jellyfin.org/openapi/jellyfin-openapi-stable.json
