# Persistence implementation

This directory contains the PostgreSQL/EF Core persistence implementation for the V1 trading platform.

The persistence boundary is intentionally isolated in `AiTrading.Infrastructure`. Domain and Application code must not depend on EF Core, Npgsql, or PostgreSQL-specific types.
