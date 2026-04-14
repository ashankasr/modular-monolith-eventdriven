---
name: dotnet-backend-modular-monolith-implement-feature
description: Use this skill when the user asks to "implement a feature end-to-end", "build a full pipeline", "add a new endpoint with everything wired up", "implement X from API to domain", "scaffold the full stack for X", or when a request clearly requires touching multiple layers (Presentation → Application → Domain → Infrastructure → Integration Events). This skill orchestrates the other specialist skills in the correct order.
---

# Full-Pipeline Feature Implementation

This skill guides end-to-end feature delivery in this .NET 9 Modular Monolith — from initial brainstorm through every layer to a working, tested HTTP endpoint.

Load `references/feature-pipeline.md` for the complete guide covering:

1. **Phase 1 — Brainstorm**: Structured questions to fully define what needs to be built before writing any code
2. **Phase 2 — Pipeline Design**: A decision matrix that determines which layers and patterns apply (domain event? integration event? saga? outbox?)
3. **Phase 3 — Implementation order**: The exact sequence in which to build each layer, with the reasoning for that order
4. **Phase 4 — Layer-by-layer implementation**: Concrete implementation steps per layer, with pointers to specialist skills for detailed patterns
5. **Phase 5 — Verification checklist**: What to confirm before calling the feature done

## How to use

When asked to implement a feature end-to-end:

1. Read the feature requirement
2. Load `references/feature-pipeline.md`
3. Work through Phase 1 (Brainstorm) and write down the answers **before generating any code** — share this analysis with the user and confirm before proceeding
4. Use the Phase 2 decision matrix to decide which layers are required
5. Implement in the Phase 3 order, delegating to specialist skills for detailed patterns
6. Confirm each item in the Phase 5 checklist

**Critical rule**: Never skip the brainstorm phase. Writing code before the design is clear produces layers that don't connect.
