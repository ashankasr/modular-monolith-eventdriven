---
name: create-module
description: This skill should be used when the user asks to "create a module", "add a new module", "scaffold a module", "generate a module", "add a bounded context", or wants to add a new feature module to the modular monolith. Generates all 5 projects (Domain, Application, Infrastructure, IntegrationEvents, Presentation) following the existing module pattern.
version: 0.1.0
---

# Create Module

Generate a complete new module for this .NET 9 Modular Monolith application.

**Usage:** `/create-module <ModuleName> [EntityName]`
- `ModuleName` — PascalCase module name, e.g. `Shipping`, `Reviews`, `Catalog`
- `EntityName` — optional PascalCase entity name; if omitted, derive from `ModuleName` (e.g. `Shipping` → `Shipment`)

Arguments: $ARGUMENTS

## Workflow

### Step 1: Parse arguments

Extract `ModuleName` (first word) and `EntityName` (second word, or derive it). Also derive `moduleName_lower` (lowercase, e.g. `shipping`) for use in SQL schema names and URL paths.

### Step 2: Generate all files

Load `references/module-templates.md` for the complete file templates and generate all files across the 5 projects:

| Project | Key files |
|---|---|
| **Domain** | `.csproj`, `{EntityName}.cs`, `Errors/{EntityName}Errors.cs`, `I{EntityName}Repository.cs` |
| **Application** | `.csproj`, `Abstractions/I{ModuleName}UnitOfWork.cs`, `Create{EntityName}/` (command + handler), `Get{ModuleName}/` (query + handler), `AssemblyReference.cs` |
| **IntegrationEvents** | `.csproj`, `{EntityName}CreatedEvent.cs`, `AssemblyReference.cs` |
| **Infrastructure** | `.csproj`, `Persistence/{ModuleName}DbContext.cs`, `Persistence/{EntityName}Repository.cs`, `Consumers/{EntityName}CreatedConsumer.cs`, `Extensions/{ModuleName}ModuleExtensions.cs`, `AssemblyReference.cs` |
| **Presentation** | `.csproj`, `{ModuleName}Endpoints.cs`, `AssemblyReference.cs` |

Use the Inventory module as the primary reference pattern — it is the cleanest example in the codebase.

### Step 3: Post-generation wiring

Load `references/post-generation-steps.md` and show the user the exact commands and code snippets needed to wire the module into the solution and API host.

## Key conventions

- Schema name = `moduleName_lower` (set via `modelBuilder.HasDefaultSchema(...)`)
- DbContext implements `I{ModuleName}UnitOfWork` (module-specific UoW, avoids DI conflicts)
- `I{EntityName}Repository` extends `IRepository<{EntityName}>` from `Common.Application`
- Module DI registered via `Add{ModuleName}Module(configuration)` extension in Infrastructure
- MediatR handlers registered via `services.AddApplication(typeof(Application.AssemblyReference).Assembly)`

## Additional Resources

- **`references/module-templates.md`** — Complete C# file templates for all 5 layers
- **`references/post-generation-steps.md`** — `dotnet sln add`, `Program.cs` wiring, EF migration commands
