# Persistence Task Checklist

## Specification

- [x] Define PostgreSQL as system of record
- [x] Define persistence boundaries and entities
- [x] Define accounting and transaction invariants
- [x] Define concurrency requirements
- [x] Define migration and testing requirements
- [ ] Review and approve persistence specification

## Implementation

- [ ] Add EF Core + PostgreSQL dependencies
- [ ] Add `TradingDbContext`
- [ ] Add persistence entities and mappings
- [ ] Add EF Core migration
- [ ] Add repository interfaces
- [ ] Add PostgreSQL repository implementations
- [ ] Replace in-memory portfolio persistence
- [ ] Replace in-memory alert persistence
- [ ] Persist market snapshots
- [ ] Persist recommendation history
- [ ] Add transaction/concurrency integration tests
- [ ] Add restart/reload integration test
- [ ] Add PostgreSQL CI service
- [ ] Update API/worker dependency registration
- [ ] Verify no provider-specific types leak into Domain/Application
- [ ] Open implementation PR
- [ ] Review implementation PR
- [ ] Merge only after explicit approval
