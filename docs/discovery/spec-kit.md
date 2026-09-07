# Discovery Spec Kit Workflow

## Purpose

This Spec Kit is the working contract between discovery and implementation.

## Requirement lifecycle

```text
Idea
 ↓
Discovery
 ↓
Proposed requirement
 ↓
Business/architecture decision
 ↓
Confirmed requirement
 ↓
Implementation spec
 ↓
Acceptance criteria
 ↓
Implementation
 ↓
Test / validation
```

## Requirement statuses

### CONFIRMED

Explicitly agreed product or architecture requirement. Implementation may rely on it.

### PROPOSED

Recommended design/default. Do not treat as immutable until confirmed.

### OPEN

Requires a product/business decision before dependent implementation.

### OUT-OF-SCOPE

Explicitly excluded from the current phase.

## Specification rules

1. Every important requirement must be testable or objectively verifiable.
2. Business assumptions must be separated from technical decisions.
3. Numerical trading/risk parameters must not be invented during implementation.
4. Financial data must be timestamped and attributable to a source.
5. AI output must be structured and validated before entering decision workflows.
6. AI cannot override deterministic risk controls.
7. Paper execution must remain isolated from live execution.
8. Provider integrations must be replaceable through interfaces/adapters.
9. Historical evaluation must prevent look-ahead bias and data leakage.
10. Discovery documents should be updated when a decision changes the baseline.

## Feature-spec template

For each future feature, document:

```text
# Feature

## Goal
## User story
## Scope
## Preconditions
## Inputs
## Processing / business rules
## Outputs
## Failure cases
## Security considerations
## Observability
## Acceptance criteria
## Open questions
```

## Implementation gate

Do not begin a major implementation until:

- Scope is understood.
- Dependencies are identified.
- Required business rules are confirmed.
- Open questions that affect correctness are resolved.
- Acceptance criteria are defined.
- Architecture boundaries are clear.

Minor implementation details may use sensible defaults and be documented.
