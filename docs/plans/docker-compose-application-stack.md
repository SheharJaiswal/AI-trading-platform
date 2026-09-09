# Docker Compose Application Stack

## Objective

Provide one command to run the Angular dashboard, .NET API, and MySQL persistence stack locally while preserving the paper-only safety boundary.

## Runtime topology

- `web`: Angular static build served by Nginx; proxies `/api` and `/health` to the API.
- `api`: .NET 10 application using MySQL persistence and committed EF Core migrations.
- `mysql`: MySQL 8.4 with a named persistent volume.

## Safety and configuration

- MySQL persistence is explicitly enabled only inside the Compose API service.
- AI is disabled by default.
- Demo market data is used by default.
- No broker credentials or AI secrets are committed.
- No live execution endpoint is introduced.

## Verification

Docker CI validates Compose syntax and builds both application images. Runtime startup waits for the MySQL health check, applies EF migrations, then starts the API; the web service waits for API health.
