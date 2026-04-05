---
name: dotnet-backend-modular-monolith-cqrs-patterns
description: Use this skill when the user asks to "create a command", "create a query", "create a handler", "add a use case", "implement a feature", "add validation", "add a domain event", "add repository methods", "map with Mapster", or asks how CQRS patterns work in this codebase.
---

# CQRS Patterns — Commands, Queries, Handlers, DDD

This skill guides implementation of any feature slice in this .NET 9 Modular Monolith.

Load `references/cqrs-patterns.md` for the complete guide covering:

1. **CQRS type hierarchy** — `ICommand`, `ICommand<T>`, `IQuery<T>` and their handlers
2. **Result/Error pattern** — `Result<T>`, `Error`, implicit conversion, all factory methods
3. **DDD entity types** — decision guide for `Entity<TKey>` vs `AuditableGuidEntity`, domain events
4. **Repository + Unit of Work** — base classes, module-specific UoW, when each is needed
5. **FluentValidation** — auto-wired via `ValidationBehavior`; how to add validators
6. **Mapster mapping** — `TypeAdapterConfig` setup, `Adapt<T>()` usage, where to register
7. **Integration Events** — publishing to MassTransit from a command handler
8. **Folder conventions** — feature-slice folder structure per operation

## How to use

When asked to implement a feature:
1. Read the feature requirement carefully
2. Load `references/cqrs-patterns.md`
3. Decide: Command (write) or Query (read)?
4. Identify which DDD types are needed
5. Generate files following the exact patterns shown in the reference

Use the **Inventory module** as the canonical reference — it is the cleanest example.
