# ADR-001: Use MySQL as the V1 System of Record

**Status:** Accepted
**Date:** 2026-09-08

## Context

The AI Trading Platform needs durable persistence for paper portfolios, positions, orders, fills, risk alerts, market snapshots, and recommendation history.

The earlier discovery and persistence material referenced PostgreSQL/TimescaleDB. The selected database for the project is now MySQL.

## Decision

Use **MySQL** as the V1 system of record.

Use **Entity Framework Core** as the .NET persistence abstraction and **Pomelo.EntityFrameworkCore.MySql** as the MySQL provider.

Store market/time-series data in MySQL tables with appropriate indexes on instrument identifiers and timestamps. Do not introduce TimescaleDB or another dedicated time-series database in V1.

Keep all MySQL/EF Core-specific implementation details inside `AiTrading.Infrastructure`. Domain and Application layers must remain database-agnostic.

## Consequences

### Positive

- Aligns the implementation with the selected MySQL environment.
- Keeps the application architecture provider-agnostic at the business layer.
- Provides durable relational transactions for paper-trading accounting.
- Avoids premature introduction of a separate time-series database.

### Trade-offs

- Time-series workloads must be designed around MySQL indexes and retention strategies.
- If future market-data volume requires specialized time-series capabilities, that change requires a new architecture decision and migration plan.

## Required follow-up

- Replace PostgreSQL/Npgsql persistence dependencies with EF Core + Pomelo MySQL.
- Use MySQL-compatible EF Core migrations.
- Add MySQL integration tests for transaction and concurrency invariants.
- Add Docker Compose support for local MySQL development when persistence infrastructure is containerized.
- Keep connection strings and credentials outside source control.

## Supersession rule

This ADR supersedes the earlier PostgreSQL/TimescaleDB assumption. Any future database change must be documented as a new ADR before implementation.
