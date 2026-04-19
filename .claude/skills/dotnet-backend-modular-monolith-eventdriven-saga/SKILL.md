---
name: dotnet-backend-modular-monolith-eventdriven-saga
description: Use this skill when the user asks to "add a saga step", "extend the saga", "add a new state", "add compensation", "add a saga transition", "how does the saga work", "add a new saga event", "modify the OrderSaga", or asks how the MassTransit state machine orchestration pattern works in this codebase.
---

# Saga Orchestration — MassTransit State Machine

This skill guides extending or understanding the MassTransit `MassTransitStateMachine<TState>` used for orchestrated distributed transactions in this codebase.

Load `references/saga-patterns.md` for the complete guide covering:

1. **Saga anatomy** — `MassTransitStateMachine<TState>`, `State`, `Event`, `InstanceState`, correlation
2. **State machine DSL** — `Initially`, `During`, `When`, `Then`, `PublishAsync`, `TransitionTo`, `Finalize`
3. **Saga state** — `SagaStateMachineInstance`, what to store, how data flows between transitions
4. **Adding a new step** — adding a state, event, transition, and compensating transaction
5. **Compensation pattern** — how to undo previous steps on failure
6. **Saga persistence** — EF Core registration, `OrdersDbContext`, optimistic concurrency
7. **Correlation** — how events are matched to the correct saga instance

## How to use

When asked to add or modify saga behaviour:
1. Load `references/saga-patterns.md`
2. Identify: new happy-path step, new failure path, or compensation?
3. Add the new `State` and `Event` properties to the saga class
4. Add the new integration event contracts to the relevant `IntegrationEvents` projects
5. Wire up `During(...).When(...).Then(...).PublishAsync(...).TransitionTo(...)`
6. Add any new properties needed to `OrderSagaState`
7. Add the new consumer in the target module (see `integration-events-consumers` skill)

The `OrderSaga` in `Orders.Application/Saga/OrderSaga.cs` is the only saga in this codebase — extend it directly.
