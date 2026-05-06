# Module File Templates

Complete C# file templates for all 5 projects. Replace placeholders:
- `{ModuleName}` → PascalCase module name (e.g. `Shipping`)
- `{EntityName}` → PascalCase entity name (e.g. `Shipment`)
- `{moduleName_lower}` → lowercase for schemas/URLs (e.g. `shipping`)

---

## 1. Domain Project

### `ModularMonolithEventDriven.Modules.{ModuleName}.Domain.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>ModularMonolithEventDriven.Modules.{ModuleName}.Domain</AssemblyName>
    <RootNamespace>ModularMonolithEventDriven.Modules.{ModuleName}.Domain</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Common\ModularMonolithEventDriven.Common.Domain\ModularMonolithEventDriven.Common.Domain.csproj" />
    <ProjectReference Include="..\..\..\Common\ModularMonolithEventDriven.Common.Application\ModularMonolithEventDriven.Common.Application.csproj" />
  </ItemGroup>
</Project>
```

### `{EntityName}.cs`
```csharp
using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Domain;

public sealed class {EntityName} : AuditableGuidEntity
{
    private {EntityName}(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public string Name { get; private set; } = string.Empty;
    // TODO: add domain-specific properties

    public static {EntityName} Create(Guid id, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return new {EntityName}(id, name);
    }
}
```

### `Errors/{EntityName}Errors.cs`
```csharp
using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Domain.Errors;

public static class {EntityName}Errors
{
    public static Error NotFound(Guid id) => new($"{ModuleName}.NotFound", $"{EntityName} with id '{id}' was not found.");
}
```

### `I{EntityName}Repository.cs`
```csharp
using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Domain;

public interface I{EntityName}Repository : IRepository<{EntityName}, Guid>
{
    Task<List<{EntityName}>> GetAllAsync(CancellationToken cancellationToken = default);
}
```

---

## 2. Application Project

### `ModularMonolithEventDriven.Modules.{ModuleName}.Application.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>ModularMonolithEventDriven.Modules.{ModuleName}.Application</AssemblyName>
    <RootNamespace>ModularMonolithEventDriven.Modules.{ModuleName}.Application</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ModularMonolithEventDriven.Modules.{ModuleName}.Domain\ModularMonolithEventDriven.Modules.{ModuleName}.Domain.csproj" />
    <ProjectReference Include="..\ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents\ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents.csproj" />
    <ProjectReference Include="..\..\..\Common\ModularMonolithEventDriven.Common.Application\ModularMonolithEventDriven.Common.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MassTransit" />
    <PackageReference Include="Mapster" />
  </ItemGroup>
</Project>
```

### `Abstractions/I{ModuleName}UnitOfWork.cs`
```csharp
using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Application.Abstractions;

public interface I{ModuleName}UnitOfWork : IUnitOfWork;
```

### `Create{EntityName}/Create{EntityName}Command.cs`
```csharp
using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Application.Create{EntityName};

public sealed record Create{EntityName}Command(string Name) : ICommand<Create{EntityName}Response>;
public sealed record Create{EntityName}Response(Guid Id);
```

### `Create{EntityName}/Create{EntityName}CommandHandler.cs`
```csharp
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.{ModuleName}.Application.Abstractions;
using ModularMonolithEventDriven.Modules.{ModuleName}.Domain;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Application.Create{EntityName};

public sealed class Create{EntityName}CommandHandler(
    I{EntityName}Repository repository,
    I{ModuleName}UnitOfWork unitOfWork) : ICommandHandler<Create{EntityName}Command, Create{EntityName}Response>
{
    public async Task<Result<Create{EntityName}Response>> Handle(
        Create{EntityName}Command command,
        CancellationToken cancellationToken)
    {
        var entity = {EntityName}.Create(Guid.NewGuid(), command.Name);
        repository.Add(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new Create{EntityName}Response(entity.Id);
    }
}
```

### `Get{ModuleName}/Get{ModuleName}Query.cs`
```csharp
using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Application.Get{ModuleName};

public sealed record Get{ModuleName}Query : IQuery<List<{EntityName}Response>>;
public sealed record {EntityName}Response(Guid Id, string Name, DateTime CreatedAt);
```

### `Get{ModuleName}/Get{ModuleName}QueryHandler.cs`
```csharp
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.{ModuleName}.Domain;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Application.Get{ModuleName};

public sealed class Get{ModuleName}QueryHandler(
    I{EntityName}Repository repository) : IQueryHandler<Get{ModuleName}Query, List<{EntityName}Response>>
{
    public async Task<Result<List<{EntityName}Response>>> Handle(
        Get{ModuleName}Query query,
        CancellationToken cancellationToken)
    {
        var items = await repository.GetAllAsync(cancellationToken);
        return items.Select(x => new {EntityName}Response(x.Id, x.Name, x.CreatedAt)).ToList();
    }
}
```

### `AssemblyReference.cs`
```csharp
namespace ModularMonolithEventDriven.Modules.{ModuleName}.Application;
public sealed class AssemblyReference;
```

---

## 3. IntegrationEvents Project

### `ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents</AssemblyName>
    <RootNamespace>ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents</RootNamespace>
  </PropertyGroup>
</Project>
```

### `{EntityName}CreatedEvent.cs`
```csharp
namespace ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents;

public sealed record {EntityName}CreatedEvent(
    Guid {EntityName}Id,
    string Name,
    DateTime CreatedAt);
```

### `AssemblyReference.cs`
```csharp
namespace ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents;
public sealed class AssemblyReference;
```

---

## 4. Infrastructure Project

### `ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure</AssemblyName>
    <RootNamespace>ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ModularMonolithEventDriven.Modules.{ModuleName}.Application\ModularMonolithEventDriven.Modules.{ModuleName}.Application.csproj" />
    <ProjectReference Include="..\..\..\Common\ModularMonolithEventDriven.Common.Infrastructure\ModularMonolithEventDriven.Common.Infrastructure.csproj" />
    <!-- Add cross-module IntegrationEvents references here as needed -->
    <!-- e.g. <ProjectReference Include="..\..\..\Modules\Orders\ModularMonolithEventDriven.Modules.Orders.IntegrationEvents\ModularMonolithEventDriven.Modules.Orders.IntegrationEvents.csproj" /> -->
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### `Persistence/{ModuleName}DbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.{ModuleName}.Application.Abstractions;
using ModularMonolithEventDriven.Modules.{ModuleName}.Domain;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Persistence;

public sealed class {ModuleName}DbContext(DbContextOptions<{ModuleName}DbContext> options)
    : BaseDbContext(options), I{ModuleName}UnitOfWork
{
    public DbSet<{EntityName}> {EntityName}s => Set<{EntityName}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("{moduleName_lower}");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof({ModuleName}DbContext).Assembly);
    }
}
```

### `Persistence/Configurations/{EntityName}Configuration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Modules.{ModuleName}.Domain;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Persistence.Configurations;

public sealed class {EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>
{
    public void Configure(EntityTypeBuilder<{EntityName}> builder)
    {
        builder.ToTable("{EntityName}s");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        // TODO: add remaining property/index/relationship configuration
    }
}
```

### `Persistence/{EntityName}Repository.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.{ModuleName}.Domain;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Persistence;

public sealed class {EntityName}Repository(
    {ModuleName}DbContext dbContext) : BaseRepository<{EntityName}, Guid, {ModuleName}DbContext>(dbContext), I{EntityName}Repository
{
    public async Task<List<{EntityName}>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.{EntityName}s.ToListAsync(cancellationToken);
}
```

### `Consumers/{EntityName}CreatedConsumer.cs`
```csharp
using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Consumers;

// Example: log when a {EntityName} is created (extend with real cross-module logic)
public sealed class {EntityName}CreatedConsumer(
    ILogger<{EntityName}CreatedConsumer> logger) : IConsumer<{EntityName}CreatedEvent>
{
    public Task Consume(ConsumeContext<{EntityName}CreatedEvent> context)
    {
        logger.LogInformation("{EntityName} created: {Id} - {Name}", context.Message.{EntityName}Id, context.Message.Name);
        return Task.CompletedTask;
    }
}
```

### `Extensions/{ModuleName}Module.cs`
```csharp
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Extensions;
using ModularMonolithEventDriven.Common.Infrastructure.Outbox;
using ModularMonolithEventDriven.Modules.{ModuleName}.Application.Abstractions;
using ModularMonolithEventDriven.Modules.{ModuleName}.Domain;
using ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Extensions;

public static class {ModuleName}Module
{
    public static IServiceCollection Add{ModuleName}Module(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<{ModuleName}DbContext>((sp, opts) =>
        {
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb"));
            opts.AddInterceptors(sp.GetRequiredService<OutboxMessagesInterceptor>());
        });

        services.AddScoped<I{ModuleName}UnitOfWork>(sp =>
            sp.GetRequiredService<{ModuleName}DbContext>());

        services.AddScoped<I{EntityName}Repository, {EntityName}Repository>();
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<{ModuleName}DbContext>>();

        services.AddApplication(typeof(Application.AssemblyReference).Assembly);

        return services;
    }

    public static void ConfigureConsumers(IRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<{EntityName}CreatedConsumer>();
    }
}
```

### `AssemblyReference.cs`
```csharp
namespace ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure;
public sealed class AssemblyReference;
```

---

## 5. Presentation Project

### `ModularMonolithEventDriven.Modules.{ModuleName}.Presentation.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>ModularMonolithEventDriven.Modules.{ModuleName}.Presentation</AssemblyName>
    <RootNamespace>ModularMonolithEventDriven.Modules.{ModuleName}.Presentation</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ModularMonolithEventDriven.Modules.{ModuleName}.Application\ModularMonolithEventDriven.Modules.{ModuleName}.Application.csproj" />
    <ProjectReference Include="..\ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure\ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### `{ModuleName}Endpoints.cs`
```csharp
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ModularMonolithEventDriven.Modules.{ModuleName}.Application.Create{EntityName};
using ModularMonolithEventDriven.Modules.{ModuleName}.Application.Get{ModuleName};

namespace ModularMonolithEventDriven.Modules.{ModuleName}.Presentation;

public static class {ModuleName}Endpoints
{
    public static IEndpointRouteBuilder Map{ModuleName}Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/{moduleName_lower}").WithTags("{ModuleName}");

        group.MapPost("/", async (Create{EntityName}Request request, ISender sender) =>
        {
            var command = new Create{EntityName}Command(request.Name);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/{moduleName_lower}/{result.Value.Id}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .WithSummary("Create a {EntityName}");

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new Get{ModuleName}Query());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithSummary("Get all {EntityName}s");

        return app;
    }

    private sealed record Create{EntityName}Request(string Name);
}
```

### `AssemblyReference.cs`
```csharp
namespace ModularMonolithEventDriven.Modules.{ModuleName}.Presentation;
public sealed class AssemblyReference;
```
