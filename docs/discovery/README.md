# Discovery Spec Kit

This directory defines the discovery baseline for the AI Trading Platform.

## Purpose

Turn the product idea into implementation-ready, testable requirements without prematurely locking business assumptions that still need validation.

## Documents

- `product-spec.md` — product vision, users, scope and outcomes
- `functional-requirements.md` — functional capabilities and acceptance criteria
- `non-functional-requirements.md` — reliability, performance, security and observability
- `architecture-spec.md` — approved architecture and technology boundaries
- `ai-spec.md` — AI/ML responsibilities, provider abstraction and prediction contracts
- `data-provider-spec.md` — external provider contracts, normalization and provenance
- `paper-trading-spec.md` — paper execution, portfolio and accounting requirements
- `risk-management-spec.md` — deterministic risk controls and decision flow
- `background-monitoring-spec.md` — autonomous monitoring, jobs and alerts
- `evaluation-spec.md` — prediction, strategy and paper-trading evaluation
- `discovery-decisions.md` — confirmed decisions and proposed defaults
- `open-questions.md` — decisions that must be resolved before implementation

## Discovery status

**Status: Discovery in progress**

Requirements marked `CONFIRMED` are part of the current baseline. Requirements marked `PROPOSED` are recommended defaults and require confirmation before being treated as product commitments. `OPEN` items require a product/business decision.

## Guiding rule

The platform is a trading platform with an AI/ML intelligence layer. AI must improve research, prediction and explanation without bypassing deterministic trading, accounting or risk controls.
