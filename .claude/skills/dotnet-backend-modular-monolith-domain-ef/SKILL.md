---
name: dotnet-backend-modular-monolith-domain-ef
description: Use this skill when the user asks to "add an entity", "model the domain", "configure EF", "add a migration", "add a DbSet", "map a table", "configure a relationship", "add an index", "add an enum property", "add a value object", "add an owned entity", "run migrations", or asks how domain entities and EF Core configuration work in this codebase.
---

# Domain Implementation, EF Configuration & Migrations

This skill guides implementation of domain entities, EF Core entity configuration inside `DbContext.OnModelCreating`, design-time factory, and EF migrations — all following the patterns established in this codebase.

Load `references/domain-ef-patterns.md` for the complete reference covering:

1. **Domain entity patterns** — aggregate roots, child entities, value objects, enums, behaviour methods
2. **EF entity configuration** — inline `OnModelCreating` blocks: keys, string lengths, precision, enum-as-string, indexes, relationships, owned entities
3. **DbContext structure** — `BaseDbContext` inheritance, `IUnitOfWork` implementation, `DbSet<T>` declarations, schema assignment
4. **Design-time factory** — `IDesignTimeDbContextFactory<T>` pattern required for `dotnet ef` CLI
5. **EF migrations** — add, apply, inspect, rollback; `__EFMigrationsHistory` per schema; auto-apply on startup

## How to use

When asked to add or change domain or persistence:
1. Read the requirement
2. Load `references/domain-ef-patterns.md`
3. Implement the domain entity first (pure C#, no EF references)
4. Add EF configuration in the module's `DbContext.OnModelCreating`
5. Register the `DbSet<T>` in the DbContext
6. Run the migration command from the `Infrastructure` project

Use **Inventory** as the primary pattern reference — it demonstrates the most EF features (owned entity, unique index, enum conversion).
