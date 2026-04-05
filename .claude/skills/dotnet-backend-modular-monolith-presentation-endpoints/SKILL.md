---
name: dotnet-backend-modular-monolith-presentation-endpoints
description: Use this skill when the user asks to "add an endpoint", "add a route", "add an API endpoint", "expose via HTTP", "add a controller", "map a request", "add a GET/POST/PUT/DELETE endpoint", "register endpoints", or asks how the Presentation layer and Minimal API work in this codebase.
---

# Presentation Layer — Minimal API Endpoints

This skill guides adding new HTTP endpoints to a module's `Presentation` project following the Minimal API pattern used throughout this codebase.

Load `references/endpoints-patterns.md` for the complete guide covering:

1. **Endpoint file structure** — static class + `IEndpointRouteBuilder` extension method
2. **Request record definitions** — co-located with the endpoint file
3. **Result → IResult mapping** — 200/201/400/404 patterns for all command/query outcomes
4. **Route grouping** — `MapGroup`, `WithTags`, `WithSummary`, `WithDescription`
5. **Registering endpoints** — `WebApplicationExtensions.cs` in the API host
6. **Presentation project dependencies** — what it references, what it must not

## How to use

When asked to add a new HTTP endpoint:
1. Load `references/endpoints-patterns.md`
2. Identify the operation: Command (POST/PUT/DELETE) or Query (GET)?
3. Add the request record and endpoint lambda in the module's `<Module>Endpoints.cs`
4. Wire it into `WebApplicationExtensions.cs` if it's a new module

Use **Inventory** (`MapInventoryEndpoints`) as the minimal reference and **Orders** (`MapOrdersEndpoints`) for the richer reference with path params and multiple verbs.
