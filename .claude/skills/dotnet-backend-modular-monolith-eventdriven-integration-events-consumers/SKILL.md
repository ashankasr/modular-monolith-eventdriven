---
name: dotnet-backend-modular-monolith-eventdriven-integration-events-consumers
description: Use this skill when the user asks to "add a consumer", "add an integration event", "subscribe to an event", "handle a message", "add a MassTransit consumer", "wire up a consumer", "how do modules communicate", or asks how integration events and consumers work in this codebase.
---

# Integration Events & Consumers

This skill guides adding new integration event contracts and the consumers that react to them — covering both orchestration (saga commands) and choreography (autonomous event reactions) patterns.

Load `references/consumers-patterns.md` for the complete guide covering:

1. **Integration event contracts** — where they live, what they contain, project reference rules
2. **Consumer anatomy** — `IConsumer<T>`, allowed dependencies, the Consume method shape
3. **Orchestration consumers** — responding to saga commands, publishing result events back
4. **Choreography consumers** — reacting autonomously to domain events, no reply required
5. **Compensation consumers** — undoing previous work on failure (e.g. `ReleaseStockCommand`)
6. **Consumer registration** — `ConfigureConsumers`, `.csproj` cross-module references
7. **Decision guide** — when to add a consumer vs a domain event handler

## How to use

When asked to add a consumer or integration event:
1. Identify the pattern: orchestration (saga sends command → consumer replies) or choreography (event published → consumer reacts)
2. Load `references/consumers-patterns.md`
3. Add the integration event contract to the correct `IntegrationEvents` project
4. Add the consumer to the correct module's `Infrastructure/Consumers/` folder
5. Register the consumer in the module's `ConfigureConsumers` method

Use **Inventory** as the primary reference — it has examples of both orchestration (`ReserveStockCommandConsumer`) and choreography (`OrderCancelledInventoryConsumer`) and compensation (`ReleaseStockCommandConsumer`).
