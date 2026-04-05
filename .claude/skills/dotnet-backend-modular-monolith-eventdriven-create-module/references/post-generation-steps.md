# Post-Generation Wiring Steps

After generating all module files, show the user these exact steps to wire everything together.

---

## 1. Add projects to the solution

```bash
dotnet sln add src/Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.Domain/ModularMonolithEventDriven.Modules.{ModuleName}.Domain.csproj
dotnet sln add src/Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.Application/ModularMonolithEventDriven.Modules.{ModuleName}.Application.csproj
dotnet sln add src/Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure/ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.csproj
dotnet sln add src/Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents/ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents.csproj
dotnet sln add src/Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.Presentation/ModularMonolithEventDriven.Modules.{ModuleName}.Presentation.csproj
```

---

## 2. Add project references to `ModularMonolithEventDriven.Api.csproj`

```xml
<ProjectReference Include="../../Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure/ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.csproj" />
<ProjectReference Include="../../Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.Presentation/ModularMonolithEventDriven.Modules.{ModuleName}.Presentation.csproj" />
```

---

## 3. Update `Program.cs`

### Add `using` statement (top of file)
```csharp
using ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Extensions;
```

### Register module services (alongside other `Add*Module` calls)
```csharp
builder.Services.Add{ModuleName}Module(builder.Configuration);
```

### Register consumers — add `{ModuleName}Module.ConfigureConsumers` to the `AddInfrastructure` array
```csharp
builder.Services.AddInfrastructure(
    [
        // ... existing modules ...
        {ModuleName}Module.ConfigureConsumers,   // ← add this line
    ],
    builder.Configuration);
```

### Add migration (inside `ApplyMigrationsAsync`, alongside other `MigrateAsync` calls)
```csharp
await scope.ServiceProvider.GetRequiredService<{ModuleName}DbContext>().Database.MigrateAsync();
```

Also add the DbContext `using`:
```csharp
using ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Persistence;
```

---

## 4. Update `WebApplicationExtensions.cs`

Add the new module's endpoint mapping to `src/Api/ModularMonolithEventDriven.Api/Extensions/WebApplicationExtensions.cs`:

```csharp
using ModularMonolithEventDriven.Modules.{ModuleName}.Presentation;

// Inside MapEndpoints():
app.Map{ModuleName}Endpoints();
```

---

## 5. Create the initial EF migration

```bash
dotnet ef migrations add Initial_{ModuleName} \
  --project src/Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure \
  --startup-project src/Modules/{ModuleName}/ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure \
  --context {ModuleName}DbContext
```

---

## 6. Verify the build

```bash
dotnet build ModularMonolithEventDriven.sln
```

---

## Notes

- Offer to make the `Program.cs`, `.csproj`, and `WebApplicationExtensions.cs` edits directly if the user wants
- Offer to run the `dotnet sln add` and `dotnet ef migrations add` commands
- Existing modules to reference if patterns are unclear: **Inventory** (cleanest), **Payments** (payment flow), **Notifications** (no UoW — read-only logging)
