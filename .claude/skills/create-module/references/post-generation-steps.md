# Post-Generation Wiring Steps

After generating all module files, show the user these exact steps to wire everything together.

---

## 1. Add projects to the solution

```bash
dotnet sln add src/Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.Domain/Ochestrator.Modules.{ModuleName}.Domain.csproj
dotnet sln add src/Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.Application/Ochestrator.Modules.{ModuleName}.Application.csproj
dotnet sln add src/Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.Infrastructure/Ochestrator.Modules.{ModuleName}.Infrastructure.csproj
dotnet sln add src/Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.IntegrationEvents/Ochestrator.Modules.{ModuleName}.IntegrationEvents.csproj
dotnet sln add src/Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.Presentation/Ochestrator.Modules.{ModuleName}.Presentation.csproj
```

---

## 2. Add project references to `Ochestrator.Api.csproj`

```xml
<ProjectReference Include="../../Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.Infrastructure/Ochestrator.Modules.{ModuleName}.Infrastructure.csproj" />
<ProjectReference Include="../../Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.Presentation/Ochestrator.Modules.{ModuleName}.Presentation.csproj" />
```

---

## 3. Update `Program.cs`

### Add `using` statements (top of file)
```csharp
using Ochestrator.Modules.{ModuleName}.Infrastructure.Extensions;
using Ochestrator.Modules.{ModuleName}.Infrastructure.Consumers;
using Ochestrator.Modules.{ModuleName}.Presentation;
```

### Register module (alongside other `Add*Module` calls)
```csharp
builder.Services.Add{ModuleName}Module(builder.Configuration);
```

### Register consumer (inside `AddMassTransit`, with other `AddConsumer` calls)
```csharp
x.AddConsumer<{EntityName}CreatedConsumer>();
```

### Map endpoints (alongside other `Map*Endpoints` calls)
```csharp
app.Map{ModuleName}Endpoints();
```

### Add migration (inside `ApplyMigrationsAsync`, alongside other `MigrateAsync` calls)
```csharp
await scope.ServiceProvider.GetRequiredService<{ModuleName}DbContext>().Database.MigrateAsync();
```

Also add the DbContext `using`:
```csharp
using Ochestrator.Modules.{ModuleName}.Infrastructure.Persistence;
```

---

## 4. Create the initial EF migration

```bash
dotnet ef migrations add Initial_{ModuleName} \
  --project src/Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.Infrastructure \
  --startup-project src/Modules/{ModuleName}/Ochestrator.Modules.{ModuleName}.Infrastructure \
  --context {ModuleName}DbContext
```

---

## 5. Verify the build

```bash
dotnet build Ochestrator.sln
```

---

## Notes

- Offer to make the `Program.cs` and `.csproj` edits directly if the user wants
- Offer to run the `dotnet sln add` and `dotnet ef migrations add` commands
- Existing modules to reference if patterns are unclear: **Inventory** (cleanest), **Payments** (payment flow), **Notifications** (no UoW — read-only logging)
