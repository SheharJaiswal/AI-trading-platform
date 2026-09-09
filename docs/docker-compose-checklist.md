# Docker Compose acceptance checklist

- [x] MySQL is isolated on the Compose network and persisted with a named volume.
- [x] API uses explicit MySQL persistence configuration.
- [x] EF migrations are applied before API startup.
- [x] Angular is served from a minimal Nginx runtime image.
- [x] Angular `/api` traffic is reverse-proxied to the API container.
- [x] API health gates web startup.
- [x] AI defaults to disabled in the Compose environment.
- [x] Demo market data is the default provider.
- [x] No live brokerage configuration is introduced.
- [x] Docker Compose configuration and image builds are CI-gated.
