# GitHub Copilot Instructions for Module Monolith Backend

## Project Overview
This is a modular monolith .NET backend architecture for the Module Monolith system, implementing Clean Architecture with Domain-Driven Design (DDD) principles.

## Architectural Overview

This is a **Modular Monolith** architecture built with .NET 9 and Minimal APIs. The system implements:

### Core Principles
- **Clean Architecture**: Strict dependency inversion with domain at the center
- **Domain-Driven Design**: Business logic encapsulated in domain entities
- **CQRS**: Command-Query Responsibility Segregation using MediatR
- **Modular Design**: Business domains separated into independent modules
- **Event-Driven Architecture**: Domain events and integration events for loose coupling
- **Database per Tenant**: Each tenant has its own isolated database for complete data separation
- **Multi-Tenancy**: Tenant context resolution through HTTP headers and tenant service

### Key Technologies
- **.NET 9** with **Minimal APIs** (no controllers)
- **MediatR** for CQRS implementation
- **Entity Framework Core** for data access with database-per-tenant
- **Mapster** for object mapping
- **.NET Aspire** for orchestration and service discovery
- **gRPC** for inter-service communication and tenant resolution
- **Redis** for caching tenant information and other data
- **Azure Service Bus** or **RabbitMQ** for messaging (configurable)
- **Serilog** for structured logging
- **Multi-tenancy** with tenant isolation via separate databases
- **Quartz.NET** for background job scheduling
- **MassTransit** for message handling

### Architecture Patterns
- **Clean Architecture Structure**: Each module follows a layered approach: Domain, Application, Infrastructure, Presentation
- **Module Organization**: Modules are organized under `ModularMonolithEventDriven.Modules.{ModuleName}.{Layer}` pattern
- **Entity Pattern**: Encapsulates business logic and validation within domain entities
- **Repository Pattern**: Abstraction for data access, following the Unit of Work pattern
- **CQRS Pattern**: Separate models for commands and queries, with MediatR handlers
- **Result Pattern**: Consistent error handling and result wrapping
- **Multi-Tenant Database per Tenant**: Each tenant has its own database with dynamic connection string resolution
- **Outbox Pattern**: Reliable event publishing using database outbox tables
- **Background Jobs**: Quartz.NET for scheduled tasks with tenant-aware processing

## High-Level Folder Structure

```
ModularMonolithEventDriven.Backend/src/
├── Api/                                   # API Host Layer
│   └── ModularMonolithEventDriven.API/                          # Main API host (composition root)
├── 
├── Aspire/                                # .NET Aspire Orchestration
│   ├── ModularMonolithEventDriven.Backend.AppHost/              # .NET Aspire orchestration
│   └── ModularMonolithEventDriven.Backend.ServiceDefaults/      # Shared Aspire service configuration
├── 
├── Common/                                # Shared Infrastructure
│   ├── ModularMonolithEventDriven.Common.Domain/                # Shared domain primitives
│   ├── ModularMonolithEventDriven.Common.Application/           # Shared application abstractions
│   ├── ModularMonolithEventDriven.Common.Infrastructure/        # Shared infrastructure implementations
│   ├── ModularMonolithEventDriven.Common.Presentation/          # Shared presentation utilities
│   └── ModularMonolithEventDriven.Common.MultiTenantProvider/   # Multi-tenancy support
├── 
└── Modules/                               # Business Modules
    ├── Customers/                         # Customer management module
    │   ├── ModularMonolithEventDriven.Modules.Customers.Domain/
    │   ├── ModularMonolithEventDriven.Modules.Customers.Application/
    │   ├── ModularMonolithEventDriven.Modules.Customers.Infrastructure/
    │   └── ModularMonolithEventDriven.Modules.Customers.Presentation/
    ├── Tenants/                           # Tenants module (master/catalog database)
    │   ├── ModularMonolithEventDriven.Modules.Tenants.Domain/
    │   ├── ModularMonolithEventDriven.Modules.Tenants.Application/
    │   ├── ModularMonolithEventDriven.Modules.Tenants.Infrastructure/
    │   ├── ModularMonolithEventDriven.Modules.Tenants.Presentation/
    │   └── ModularMonolithEventDriven.Modules.Tenants.IntegrationEvents/
    ├── Users/                             # User management module
    │   ├── ModularMonolithEventDriven.Modules.Users.Domain/
    │   ├── ModularMonolithEventDriven.Modules.Users.Application/
    │   ├── ModularMonolithEventDriven.Modules.Users.Infrastructure/
    │   ├── ModularMonolithEventDriven.Modules.Users.Presentation/
    │   └── ModularMonolithEventDriven.Modules.Users.IntegrationEvents/
    └── Notifications/                     # Notifications module
        ├── ModularMonolithEventDriven.Modules.Notifications.Domain/
        ├── ModularMonolithEventDriven.Modules.Notifications.Application/
        ├── ModularMonolithEventDriven.Modules.Notifications.Infrastructure/
        └── ModularMonolithEventDriven.Modules.Notifications.Presentation/
```

### Module Structure Pattern
Each business module follows this consistent structure:
```
ModularMonolithEventDriven.Modules.{ModuleName}.{Layer}/
├── Domain/                                # Core business logic
│   ├── {Entity}.cs                        # Domain entities
│   ├── {Entity}Errors.cs                  # Domain-specific errors
│   ├── Events/                            # Domain events
│   └── I{Entity}Repository.cs             # Repository interfaces
├── Application/                           # Use cases and handlers
│   ├── {Entity}/                          # Entity-specific operations
│   │   ├── Create{Entity}/                # Command handlers
│   │   ├── Get{Entity}/                   # Query handlers
│   │   └── Update{Entity}/                # Command handlers
│   ├── Grpc/                              # gRPC service implementations
│   ├── Options/                           # Application settings/options
│   └── Data/                              # Application data contracts
├── Infrastructure/                        # External concerns
│   ├── Data/                              # EF Core configurations
│   ├── Repositories/                      # Repository implementations
│   └── {ModuleName}Module.cs              # Module registration
├── Presentation/                          # HTTP/gRPC API layer
│   ├── {Entity}/                          # REST endpoints
│   │   ├── Create{Entity}.cs              # Endpoint implementations
│   │   ├── Get{Entity}.cs                 # Endpoint implementations
│   │   └── Update{Entity}.cs              # Endpoint implementations
│   └── Grpc/                              # gRPC endpoints
│       └── {Entity}ServiceGrpcEndpoint.cs # gRPC service registrations
└── IntegrationEvents/                     # Cross-module communication
    ├── {Entity}Created.cs                 # Integration event definitions
    └── {Entity}CreatedHandler.cs          # Integration event handlers
```

## Coding Standards

### Naming Conventions
- Use PascalCase for classes, methods, properties, and namespaces
- Use camelCase for private fields and parameters
- Prefix interfaces with 'I' (e.g., `ITempObjectRepository`)
- Use descriptive names that reflect business domain

### File Organization
- One class per file
- File name should match the class name
- Group related classes in appropriate folders within each layer

### Dependencies
- Domain layer should have NO dependencies on other layers
- Application layer can depend on Domain only
- Infrastructure layer can depend on Domain and Application
- Presentation layer can depend on Application and Domain

## Common Patterns

### Domain Entity Base Classes

The system provides a rich hierarchy of base entity classes to choose from based on your requirements:

#### Base Entity Classes (Non-Generic)
- **`Entity`**: Basic entity with domain event support
  - Use when: You need a simple entity with just domain events, no ID management
  - Features: Domain event raising and clearing

- **`AuditableEntity`**: Entity with audit tracking
  - Use when: You need creation and modification tracking without soft delete
  - Features: `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` + domain events

- **`SoftDeletableEntity`**: Entity with soft delete capability
  - Use when: You need soft delete without audit tracking
  - Features: `IsDeleted`, `Delete()`, `Restore()` methods + domain events

- **`AuditableSoftDeletableEntity`**: Entity with both audit tracking and soft delete
  - Use when: You need full audit tracking with soft delete capability
  - Features: All auditable properties + `IsDeleted` + domain events

#### Generic Entity Classes (With Strongly-Typed IDs)
- **`Entity<TKey>`**: Base entity with generic primary key
  - Use when: You need control over the ID type (Guid, int, long, string)
  - Features: Generic `Id` property + domain events + equality comparison

- **`GuidEntity`**: Entity with Guid primary key (inherits `Entity<Guid>`)
  - Use when: You need Guid IDs with automatic generation
  - Features: Auto-generated Guid ID + domain events

- **`IntEntity`**: Entity with int primary key (inherits `Entity<int>`)
  - Use when: You need database-generated sequential integer IDs
  - Features: Auto-generated int ID + domain events

- **`LongEntity`**: Entity with long primary key (inherits `Entity<long>`)
  - Use when: You need database-generated sequential long IDs
  - Features: Auto-generated long ID + domain events

#### Auditable Generic Entity Classes
- **`AuditableGuidEntity`**: Guid entity with audit tracking
  - Use when: You need Guid IDs with full audit tracking but no soft delete
  - Features: Guid ID + all auditable properties + domain events
  - **Most common for reference data and lookup tables**

- **`AuditableIntEntity`**: Int entity with audit tracking
  - Use when: You need database-generated int IDs with audit tracking
  - Features: Auto-generated int ID + all auditable properties + domain events

- **`AuditableLongEntity`**: Long entity with audit tracking
  - Use when: You need database-generated long IDs with audit tracking
  - Features: Auto-generated long ID + all auditable properties + domain events

#### Auditable Soft Deletable Generic Entity Classes
- **`AuditableSoftDeletableGuidEntity`**: Guid entity with audit tracking and soft delete
  - Use when: You need Guid IDs with full audit tracking and soft delete
  - Features: Guid ID + all auditable properties + `IsDeleted` + domain events
  - **Most common for business entities (users, customers, orders, etc.)**

- **`AuditableSoftDeletableIntEntity`**: Int entity with audit tracking and soft delete
  - Use when: You need database-generated int IDs with full features
  - Features: Auto-generated int ID + all auditable properties + `IsDeleted` + domain events

- **`AuditableSoftDeletableLongEntity`**: Long entity with audit tracking and soft delete
  - Use when: You need database-generated long IDs with full features
  - Features: Auto-generated long ID + all auditable properties + `IsDeleted` + domain events

#### Soft Deletable Generic Entity Classes
- **`SoftDeletableGuidEntity`**: Guid entity with soft delete only
  - Use when: You need Guid IDs with soft delete but no audit tracking
  - Features: Guid ID + `IsDeleted` + domain events

- **`SoftDeletableIntEntity`**: Int entity with soft delete only
  - Use when: You need database-generated int IDs with soft delete but no audit tracking
  - Features: Auto-generated int ID + `IsDeleted` + domain events

- **`SoftDeletableLongEntity`**: Long entity with soft delete only
  - Use when: You need database-generated long IDs with soft delete but no audit tracking
  - Features: Auto-generated long ID + `IsDeleted` + domain events

### Entity Pattern Examples

**Most Common Pattern (Business Entities):**
```csharp
public sealed class User : AuditableSoftDeletableGuidEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    [Encrypted]  // Use for sensitive data
    public string? Secret { get; set; }
    
    // Factory method for creation
    public static Result<User> Create(
        string firstName,
        string lastName,
        string email,
        string createdBy,
        DateTime occurredOnUtc)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            CreatedBy = createdBy,
            ModifiedBy = createdBy,
            CreatedAt = occurredOnUtc,
            ModifiedAt = occurredOnUtc
        };
        
        // Raise domain events
        user.Raise(new UserCreatedDomainEvent(user.Id, createdBy, occurredOnUtc));
        
        return user;
    }
    
    // Domain methods for business operations
    public Result Deactivate(string deactivatedBy, DateTime occurredOnUtc)
    {
        IsActive = false;
        ModifiedBy = deactivatedBy;
        ModifiedAt = occurredOnUtc;
        
        Raise(new UserDeactivatedDomainEvent(Id, Email, deactivatedBy, occurredOnUtc));
        
        return Result.Success();
    }
    
    public Result Update(string firstName, string lastName, string modifiedBy, DateTime occurredOnUtc)
    {
        FirstName = firstName;
        LastName = lastName;
        ModifiedBy = modifiedBy;
        ModifiedAt = occurredOnUtc;
        
        Raise(new UserUpdatedDomainEvent(Id, modifiedBy, occurredOnUtc));
        
        return Result.Success();
    }
}
```

**Lookup/Reference Data Pattern:**
```csharp
public sealed class Category : AuditableGuidEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    public static Result<Category> Create(
        string name,
        string description,
        string createdBy,
        DateTime occurredOnUtc)
    {
        var category = new Category
        {
            Name = name,
            Description = description,
            CreatedBy = createdBy,
            ModifiedBy = createdBy,
            CreatedAt = occurredOnUtc,
            ModifiedAt = occurredOnUtc
        };
        
        category.Raise(new CategoryCreatedDomainEvent(category.Id));
        
        return category;
    }
}
```

**Simple Entity Pattern (No Audit):**
```csharp
public sealed class Tag : GuidEntity
{
    public string Name { get; set; } = string.Empty;
    
    public static Result<Tag> Create(string name)
    {
        var tag = new Tag { Name = name };
        tag.Raise(new TagCreatedDomainEvent(tag.Id));
        return tag;
    }
}
```

### Repository Pattern
```csharp
public interface IEntityRepository
{
    Task<Entity?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Entity>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(Entity entity);
    void Update(Entity entity);
    void Delete(Entity entity);
}
```

### Entity Configuration Pattern

The system provides matching configuration base classes for all entity types. These handle the boilerplate EF Core configuration for IDs, audit fields, and soft delete properties.

#### Configuration Base Classes (Generic)

**For Generic Primary Key Entities:**
- **`EntityConfiguration<TEntity, TKey>`**: Base configuration for `Entity<TKey>`
  - Configures primary key based on type (Guid, int, long, string)
  - Override `ConfigureEntity(builder)` for entity-specific properties

- **`GuidEntityConfiguration<TEntity>`**: Configuration for `GuidEntity`
  - Pre-configured for Guid primary keys
  - Override `ConfigureEntity(builder)` for entity-specific properties

- **`IntEntityConfiguration<TEntity>`**: Configuration for `IntEntity`
  - Pre-configured for auto-incrementing int primary keys
  - Override `ConfigureEntity(builder)` for entity-specific properties

**For Auditable Generic Entities:**
- **`AuditableEntityConfiguration<TEntity, TKey>`**: Configuration for auditable entities
  - Configures ID + CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
  - Override `ConfigureAuditableEntity(builder)` for entity-specific properties

- **`AuditableGuidEntityConfiguration<TEntity>`**: Configuration for `AuditableGuidEntity`
  - Pre-configured Guid ID + all audit fields
  - Override `ConfigureAuditableGuidEntity(builder)` for entity-specific properties

- **`AuditableLongEntityConfiguration<TEntity>`**: Configuration for `AuditableLongEntity`
  - Pre-configured long ID (identity) + all audit fields
  - Override `ConfigureAuditableLongEntity(builder)` for entity-specific properties

**For Soft Deletable Generic Entities:**
- **`SoftDeletableEntityConfiguration<TEntity, TKey>`**: Configuration for soft deletable entities
  - Configures ID + IsDeleted
  - Override `ConfigureSoftDeletableEntity(builder)` for entity-specific properties

- **`SoftDeletableGuidEntityConfiguration<TEntity>`**: Configuration for `SoftDeletableGuidEntity`
  - Pre-configured Guid ID + IsDeleted
  - Override `ConfigureSoftDeletableGuidEntity(builder)` for entity-specific properties

**For Auditable Soft Deletable Generic Entities:**
- **`AuditableSoftDeletableEntityConfiguration<TEntity, TKey>`**: Configuration for full-featured entities
  - Configures ID + all audit fields + IsDeleted
  - Override `ConfigureAuditableSoftDeletableEntity(builder)` for entity-specific properties

- **`AuditableSoftDeletableGuidEntityConfiguration<TEntity>`**: Configuration for `AuditableSoftDeletableGuidEntity`
  - Pre-configured Guid ID + all audit fields + IsDeleted
  - Override `ConfigureAuditableSoftDeletableGuidEntity(builder)` for entity-specific properties
  - **Most commonly used configuration class**

#### Configuration Pattern Examples

**Most Common Pattern (Full-Featured Business Entity):**
```csharp
using ModularMonolithEventDriven.Common.Infrastructure.Data;
using ModularMonolithEventDriven.Modules.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ModularMonolithEventDriven.Modules.Users.Infrastructure.Data.Configurations.Users;

public sealed class UserConfiguration : AuditableSoftDeletableGuidEntityConfiguration<User>
{
    protected override void ConfigureAuditableSoftDeletableGuidEntity(EntityTypeBuilder<User> builder)
    {
        // Table configuration
        builder.ToTable(nameof(User));

        // Property configurations
        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("nvarchar(255)");

        // Encrypted property (nullable)
        builder.Property(x => x.Secret)
            .HasMaxLength(4000) // Encrypted data is larger
            .HasColumnType("nvarchar(4000)")
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(x => x.Email).IsUnique();
        
        // Note: Id, audit fields (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy),
        // and IsDeleted are already configured by the base class
    }
}
```

**Alternative Pattern Using Configure Override (Less Common):**
```csharp
public sealed class UserConfiguration : AuditableSoftDeletableGuidEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        // Table configuration
        builder.ToTable(nameof(User));

        // Custom primary key configuration (if needed)
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();

        // Property configurations
        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        // IMPORTANT: Call base.Configure to apply audit and soft delete configuration
        base.Configure(builder);
    }
}
```

**Lookup/Reference Data Configuration:**
```csharp
public sealed class RoleConfiguration : AuditableSoftDeletableGuidEntityConfiguration<Role>
{
    protected override void ConfigureAuditableSoftDeletableGuidEntity(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(nameof(Role));

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(x => x.IsSystemRole)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Add unique constraint
        builder.HasIndex(x => x.Name).IsUnique();

        // Seed data
        builder.HasData(new
        {
            Id = new Guid("2F46319B-A050-4791-9D5F-9454C5019257"),
            Name = "Admin",
            CreatedAt = new DateTime(2025, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            CreatedBy = "System",
            IsSystemRole = true,
            IsActive = true,
            IsDeleted = false,
            Description = "Administrator role with full access"
        });
    }
}
```

**Simple Entity Configuration (No Audit/Soft Delete):**
```csharp
public sealed class TagConfiguration : GuidEntityConfiguration<Tag>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable(nameof(Tag));

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);
    }
}
```

#### Configuration Best Practices

1. **Choose the Right Override Method**:
   - Use `Configure{EntityType}()` override (e.g., `ConfigureAuditableSoftDeletableGuidEntity`) - **Preferred**
   - Only use `Configure()` override if you need complete control over the base configuration

2. **File Organization**:
   - Place configurations in `Infrastructure/Data/Configurations/{Entity}/` folders
   - One configuration class per entity
   - Name pattern: `{EntityName}Configuration.cs`

3. **Always Specify Column Types**:
   - Use explicit column types for consistency: `.HasColumnType("nvarchar(100)")`
   - Common types: `nvarchar(n)`, `int`, `bigint`, `bit`, `datetime2(7)`, `uniqueidentifier`

4. **Table Names**:
   - Use `builder.ToTable(nameof(Entity))` for consistency
   - Apply module schema: `builder.ToTable(nameof(Entity), Schemas.ModuleName)`

5. **Encrypted Properties**:
   - Mark with `[Encrypted]` attribute in domain entity
   - Use larger column sizes in configuration (4000 chars typical)
   - Make nullable if appropriate: `.IsRequired(false)`

6. **Seed Data**:
   - Use anonymous objects with all required properties
   - Include explicit values for audit fields in seed data
   - Set `IsDeleted = false` for soft deletable entities

7. **Indexes**:
   - Add unique indexes for business keys: `builder.HasIndex(x => x.Email).IsUnique()`
   - Consider composite indexes for common query patterns

8. **Relationships**:
   - Configure in the principal entity's configuration
   - Use Fluent API for complex relationships
   - Consider cascade delete behavior with soft deletes

### Endpoint Pattern
```csharp
internal sealed class CreateEntity : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("entities", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new CreateEntityCommand(request.Name));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(AuthPolicyNames.CreateEntities) // If authorization required
        .WithTags(Tags.Entities)
        .WithSummary("Create a new entity")
        .WithDescription("Creates a new entity in the system. Requires the CreateEntities permission.");
    }

    internal sealed class Request
    {
        public string Name { get; set; } = string.Empty;
        // Other properties
    }
}
```

### AssemblyReference Pattern
Each layer has an AssemblyReference class for reflection:
```csharp
using System.Reflection;

namespace ModularMonolithEventDriven.Modules.{ModuleName}.{Layer};

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
```

### Schemas Pattern
Each module defines its database schema:
```csharp
namespace ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Data;

internal static class Schemas
{
    internal const string {ModuleName} = "schema_name";
}
```

Current schema mappings:
- Users: `"auth"`
- Customers: `"customer"`
- Notifications: `"noti"`
- Tenants: Uses default schema (no custom schema)

### Options Pattern
Use strongly-typed options classes for module configuration:
```csharp
namespace ModularMonolithEventDriven.Modules.{ModuleName}.Application.Options;

/// <summary>
/// Configuration settings for the {ModuleName} module.
/// </summary>
public sealed class {ModuleName}Options
{
    /// <summary>
    /// Configuration property with default value.
    /// </summary>
    public int SomeProperty { get; set; } = 3;
}
```

Register in module:
```csharp
services.Configure<{ModuleName}Options>(
    configuration.GetSection("{ModuleName}"));
```

Use in handlers:
```csharp
internal sealed class SomeCommandHandler(
    IOptions<{ModuleName}Options> options) : ICommandHandler<SomeCommand, Result>
{
    private readonly {ModuleName}Options _options = options.Value;
    
        public async Task<Result> Handle(SomeCommand command, CancellationToken cancellationToken)
    {
        // Use _options.SomeProperty
    }
}
```

### CQRS Pattern
- Commands: Use for state-changing operations
- Queries: Use for data retrieval operations
- Handlers: One handler per command/query
- **Do NOT inject `ITenantContextAccessor` in command/query handlers** - tenant context flows automatically via middleware
- Domain entities should NOT receive tenant parameters - keep domain logic pure

**Command Handler Pattern:**
```csharp
public record CreateEntityCommand(
    string Name,
    string Email) : ICommand<Guid>;

public sealed class CreateEntityCommandHandler(
    IEntityRepository entityRepository,
    IEntityUnitOfWork unitOfWork,
    IUserHttpContext userContext) : ICommandHandler<CreateEntityCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateEntityCommand command, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        
        // Check if entity with email already exists
        var existingEntity = await entityRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingEntity is not null)
        {
            return Result.Failure<Guid>(EntityErrors.EmailNotUnique(command.Email));
        }
        
        // Domain entity Create method does NOT receive tenant parameters
        Result<Entity> entityResult = Entity.Create(
            command.Name,
            command.Email,
            userContext.Username,
            now);
        
        if (entityResult.IsFailure)
        {
            return Result.Failure<Guid>(entityResult.Error);
        }
        
        entityRepository.Add(entityResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return entityResult.Value.Id;
    }
}
```

**Query Handler Pattern:**
```csharp
public record GetEntityQuery(Guid Id) : IQuery<EntityResponse>;

public sealed class GetEntityQueryHandler(IEntityRepository entityRepository)
    : IQueryHandler<GetEntityQuery, EntityResponse>
{
    public async Task<Result<EntityResponse>> Handle(GetEntityQuery query, CancellationToken cancellationToken)
    {
        Entity? entity = await entityRepository.GetAsync(query.Id, cancellationToken);
        
        if (entity is null)
        {
            return Result.Failure<EntityResponse>(EntityErrors.NotFound(query.Id));
        }
        
        return entity.Adapt<EntityResponse>();
    }
}
```

**Key Points:**
- Command/Query handlers focus on business logic, not tenant management
- Tenant context is automatically available to DbContext via `ITenantContextAccessor`
- Domain entities remain pure with no infrastructure dependencies
- Validation and business rules are enforced in handlers and domain entities

### Result Pattern
Always use the Result pattern for error handling:
```csharp
public static Result<T> Success<T>(T value) => new(value, true, Error.None);
public static Result Failure(Error error) => new(false, error);
```

## Integration Events
Use integration events for cross-module communication. All integration events must be tenant-aware to ensure proper context flows through asynchronous messaging.

## Multi-Tenant Message Producers and Consumers

### Overview
The system uses a multi-tenant aware messaging pattern to ensure proper tenant context flows through asynchronous integration events published via MassTransit (Azure Service Bus or RabbitMQ).

### Integration Event Base Class
All integration events inherit from `IntegrationEvent` which includes tenant context:
```csharp
public abstract class IntegrationEvent(Guid id, DateTime occurredOnUtc, string? tenantIdentifier = null) : IIntegrationEvent
{
    public Guid Id { get; init; } = id;
    public DateTime OccurredOnUtc { get; init; } = occurredOnUtc;
    public string? TenantIdentifier { get; init; } = tenantIdentifier;
}
```

### Creating Tenant-Aware Integration Events
When defining integration events, always include `tenantIdentifier` in the constructor and pass it to the base class:
```csharp
public sealed class OrganizationCreatedIntegrationEvent : IntegrationEvent
{
    public OrganizationCreatedIntegrationEvent(
        Guid id,
        DateTime occurredOnUtc,
        string tenantIdentifier,
        Guid organizationId)
        : base(id, occurredOnUtc, tenantIdentifier)
    {
        OrganizationId = organizationId;
    }

    public Guid OrganizationId { get; init; }
}
```

### Publishing Integration Events (Producers)
Domain event handlers publish integration events using `IEventBus` and include tenant context from `ITenantContextAccessor`:
```csharp
internal sealed class OrganizationCreatedDomainEventHandler(
    IEventBus eventBus,
    ITenantContextAccessor tenantContextAccessor)
    : IDomainEventHandler<OrganizationCreatedDomainEvent>
{
    public async Task Handle(OrganizationCreatedDomainEvent @event, CancellationToken cancellationToken)
    {
        await eventBus.PublishAsync(new OrganizationCreatedIntegrationEvent(
            @event.Id,
            @event.OccurredOnUtc,
            tenantContextAccessor.Current!.Identifier,  // Include tenant identifier
            @event.OrganizationId),
            cancellationToken);
    }
}
```

**Key Points for Producers:**
- Always inject `ITenantContextAccessor` to access the current tenant context
- Pass `tenantContextAccessor.Current!.Identifier` when creating integration events
- Use `IEventBus.PublishAsync()` to publish events

### Consuming Integration Events (Consumers)
Consumers inherit from `TenantAwareIntegrationEventConsumer<T>` which automatically sets tenant context:
```csharp
public sealed class OrganizationCreatedIntegrationEventConsumer(
    ISender sender,
    OrganizationGrpcService.OrganizationGrpcServiceClient organizationServiceClient,
    ILogger<OrganizationCreatedIntegrationEventConsumer> logger,
    ITenantContextInitializer tenantInit,
    ITenantContextAccessor tenantContextAccessor)
    : TenantAwareIntegrationEventConsumer<OrganizationCreatedIntegrationEvent>(tenantInit)
{
    protected override async Task ConsumeMessage(ConsumeContext<OrganizationCreatedIntegrationEvent> context)
    {
        var @event = context.Message;
        
        // Tenant context is already set by the base class
        // Database connections will automatically use the correct tenant database
        
        // Access current tenant if needed
        var tenantIdentifier = tenantContextAccessor.Current!.Identifier;
        
        // Make gRPC calls with tenant context in headers
        var headers = new Metadata
        {
            { "X-TenantIdentifier", tenantIdentifier }
        };
        
        var response = await organizationServiceClient
            .GetOrganizationAsync(request, headers, cancellationToken: context.CancellationToken);
        
        // Process the event...
    }
}
```

**Key Points for Consumers:**
- Inherit from `TenantAwareIntegrationEventConsumer<T>` (not `IConsumer<T>`)
- Inject `ITenantContextInitializer` and pass to base constructor
- Implement `ConsumeMessage()` instead of `Consume()`
- Tenant context is automatically established before `ConsumeMessage()` is called
- Tenant context is automatically cleaned up after processing
- DbContext and repositories will automatically use the correct tenant database
- For gRPC calls, pass tenant identifier in metadata headers

### How It Works
1. **Base Consumer (`TenantAwareIntegrationEventConsumer<T>`)**: 
   - Extracts `TenantIdentifier` from the integration event
   - Uses `ITenantContextInitializer.UseTenantAsync()` to establish tenant context
   - Calls `ConsumeMessage()` with tenant context active
   - Automatically cleans up tenant context after processing

2. **Tenant Context Propagation**:
   - `ITenantContextAccessor` uses `AsyncLocal<TenantContext>` to flow context through async operations
   - DbContext resolves connection strings based on `ITenantContextAccessor.Current`
   - No manual tenant context management needed in business logic

3. **Null Tenant Handling**:
   - If `TenantIdentifier` is null or empty, the consumer processes without setting tenant context
   - Useful for tenant-independent events (e.g., system-level events)

### Consumer Registration
Consumers are automatically registered by MassTransit when you configure the module. In `InfrastructureConfiguration.cs`:
```csharp
services.AddMassTransit(configure =>
{
    // Consumers are auto-registered from assemblies
    foreach (Action<IRegistrationConfigurator> configureConsumers in moduleConfigureConsumers)
    {
        configureConsumers(configure);
    }
    
    // Configure broker (RabbitMQ or Azure Service Bus)
    // ...
});
```

### Best Practices
- **Always include tenant identifier** when creating integration events in domain event handlers
- **Use `TenantAwareIntegrationEventConsumer<T>`** as the base class for all multi-tenant consumers
- **Never manually set tenant context** in consumers - let the base class handle it
- **Pass tenant identifier in gRPC headers** when making cross-module calls from consumers
- **Validate tenant context** when processing critical operations (check for null)
- **Use `ITenantContextAccessor.Current`** to access tenant information in handlers/services

## Module Registration
Each module registers its dependencies using extension methods following this pattern:
```csharp
public static class ModuleExtensions
{
    public static IServiceCollection Add{ModuleName}Module(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        
        // Register endpoints
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        
        // Register gRPC endpoints (if applicable)
        services.AddGrpcEndpoints(Presentation.AssemblyReference.Assembly);
        
        // Register background jobs
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
        
        // Configure module-specific options
        services.Configure<{ModuleName}Options>(
            configuration.GetSection("{ModuleName}"));
        
        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database configuration with tenant-aware connection string using ITenantContextAccessor
        services.AddDbContext<{ModuleName}DbContext>((sp, options) =>
        {
            var tenantCtx = sp.GetRequiredService<ITenantContextAccessor>().Current;

            if (tenantCtx == null)
                throw new InvalidOperationException("TenantContext is not set for this scope.");

            var databaseName = tenantCtx.DatabaseName;

            var tenantDbConnectionFormat = configuration.GetConnectionString("TenantConnectionFormat");
            var connectionString = databaseName != null ?
                string.Format(CultureInfo.InvariantCulture, tenantDbConnectionFormat!, databaseName)
                : configuration.GetConnectionString("TenantOneDatabase");

            options.UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.{ModuleName}))
                .AddInterceptors(sp.GetRequiredService<OutboxMessagesInterceptor>());
        });

        // Register repositories and services
        services.AddScoped<I{ModuleName}UnitOfWork>(sp => sp.GetRequiredService<{ModuleName}DbContext>());
        services.AddScoped<I{Entity}Repository, {Entity}Repository>();
    }
}
```

**Note for Tenant Module**: The Tenants module is special as it uses the catalog database (CatalogTenantDatabase) rather than tenant-specific databases:
```csharp
services.AddDbContext<TenantDbContext>((sp, options) =>
    options
        .UseSqlServer(
        configuration.GetConnectionString("CatalogTenantDatabase"),
        sqlServerOptions => sqlServerOptions
            .MigrationsHistoryTable(HistoryRepository.DefaultTableName))
        .AddInterceptors(sp.GetRequiredService<OutboxMessagesInterceptor>()));
```

## Result Pattern Extensions
The system uses extension methods for clean result handling:
```csharp
// Using Match for clean result handling
return result.Match(Results.Ok, ApiResults.Problem);

// Error handling in domain layer
if (entityResult.IsFailure)
{
    return Result.Failure<Guid>(entityResult.Error);
}
```

## Tags Class Pattern
Use a Tags class for consistent API documentation:
```csharp
internal static class Tags
{
    internal const string Users = "Users";
    internal const string Roles = "Roles";
    internal const string Permissions = "Permissions";
    internal const string Customers = "Customers";
    internal const string Tenants = "Tenants";
    internal const string Notifications = "Notifications";
}
```

## Infrastructure Configuration

### Common Infrastructure Setup
The `InfrastructureConfiguration` class provides shared infrastructure services:
```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    Action<IRegistrationConfigurator>[] moduleConfigureConsumers,
    IConfiguration configuration)
{
    // Core services
    services.AddSingleton<IEventBus, EventBus.EventBus>();
    services.AddScoped<TenantProvider>();
    services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
    services.AddScoped<ITenantDatabaseNameResolver, TenantDatabaseNameResolver>();
    services.AddScoped<ITenantContextInitializer, TenantContextInitializer>();
    services.AddHttpContextAccessor();
    
    // Caching with Redis
    services.TryAddSingleton<ICacheService, CacheService>();
    var cacheConStr = configuration.GetConnectionString("Cache")!;
    IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(cacheConStr!);
    services.TryAddSingleton(connectionMultiplexer);
    
    // Background job scheduling
    services.AddQuartz();
    services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    
    // Message broker (configurable: RabbitMQ or Azure Service Bus)
    services.AddMassTransit(configure =>
    {
        foreach (Action<IRegistrationConfigurator> configureConsumers in moduleConfigureConsumers)
        {
            configureConsumers(configure);
        }
        
        var useRabbitMQ = configuration.GetValue<bool>("MessageBroker:UseRabbitMQ", false);
        if (useRabbitMQ)
        {
            ConfigureRabbitMQ(configure, configuration);
        }
        else
        {
            ConfigureAzureServiceBus(configure, configuration);
        }
    });
    
    return services;
}
```

### Message Broker Configuration
The system supports both RabbitMQ and Azure Service Bus:
- Set `MessageBroker:UseRabbitMQ` to `true` for RabbitMQ
- Default is Azure Service Bus
- Configuration handled automatically based on appsettings

### Middleware Pipeline
The application uses the following middleware order:
1. `GlobalExceptionHandlingMiddleware` - Global exception handling
2. `TenantContextMiddleware` - Resolves tenant context from HTTP headers and stores in AsyncLocal
3. `TenantValidationMiddleware` - Validates tenant existence and handles errors
4. Authentication/Authorization
5. `LocalizationMiddleware`

### gRPC Interceptor
- `TenantGrpcInterceptor` - Extracts tenant identifier from gRPC metadata headers and establishes tenant context for gRPC calls using `ITenantContextInitializer`

## Configuration
- Use strongly-typed options classes with the suffix "Options" (e.g., `NotificationOptions`)
- Place options classes in the `Options/` folder within the Application layer
- Follow the pattern in `InfrastructureConfiguration.cs` for infrastructure configuration
- Register options using `services.Configure<TOptions>(configuration.GetSection("SectionName"))`
- Use `IOptions<TOptions>` for dependency injection
- Register services using extension methods

## Performance and Caching
- Use async/await consistently
- Implement caching where appropriate using Redis through `ICacheService`
- Use pagination for large data sets
- Optimize database queries
- Cache tenant information for background jobs (1-day expiration by default)
- Use connection pooling for database connections

## Multi-Tenancy Architecture

### AsyncLocal-Based Tenant Context Pattern
The system uses an **AsyncLocal-based pattern** for tenant context management:
- **`ITenantContextAccessor`**: Stores tenant context in `AsyncLocal<TenantContext>` for automatic flow through async operations
- **`ITenantContextInitializer`**: Establishes tenant context for a scope with automatic restoration
- **`ITenantDatabaseNameResolver`**: Resolves database names with caching (10-minute sliding expiration)
- **`TenantContext`**: Immutable record containing `Identifier` and `DatabaseName`

### Database per Tenant
- Each tenant has its own dedicated database for complete data isolation
- Tenant information is stored in a centralized catalog database (`CatalogTenantDatabase`)
- Dynamic connection string resolution based on tenant context stored in `AsyncLocal`

### Tenant Resolution
- **HTTP Requests**: Tenants identified through `X-TenantIdentifier` HTTP header
- **gRPC Calls**: Tenants identified through `X-TenantIdentifier` metadata header
- **Middleware Flow**:
  1. `TenantContextMiddleware` resolves tenant from headers and stores in `HttpContext.Items` and `ITenantContextAccessor`
  2. `TenantValidationMiddleware` validates tenant existence and handles errors
- **gRPC Flow**: `TenantGrpcInterceptor` uses `ITenantContextInitializer.UseTenantAsync()` to establish context

### Connection String Management
```csharp
// Dynamic tenant database resolution using AsyncLocal-based ITenantContextAccessor
services.AddDbContext<ModuleDbContext>((sp, options) =>
{
    var tenantCtx = sp.GetRequiredService<ITenantContextAccessor>().Current;

    if (tenantCtx == null)
        throw new InvalidOperationException("TenantContext is not set for this scope.");

    var databaseName = tenantCtx.DatabaseName;

    var tenantDbConnectionFormat = configuration.GetConnectionString("TenantConnectionFormat");
    var connectionString = databaseName != null ?
        string.Format(CultureInfo.InvariantCulture, tenantDbConnectionFormat!, databaseName)
        : configuration.GetConnectionString("TenantOneDatabase");

    options.UseSqlServer(
        connectionString,
        // Module-specific schema for migrations
        sqlServerOptions => sqlServerOptions
            .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.ModuleName))
        .AddInterceptors(sp.GetRequiredService<OutboxMessagesInterceptor>());
});
```

### Background Jobs and Multi-Tenancy
- Background jobs (Quartz.NET) process all tenants
- Jobs iterate through tenant list retrieved from gRPC service
- Tenant context is set per tenant during job execution
- Cached tenant list for performance optimization

### Tenant Context Services
- `ITenantContextAccessor` / `TenantContextAccessor`: AsyncLocal storage for tenant context, accessible throughout the async execution flow
- `ITenantContextInitializer` / `TenantContextInitializer`: Sets up tenant context for a specific scope with automatic cleanup
- `ITenantDatabaseNameResolver` / `TenantDatabaseNameResolver`: Resolves database name from tenant identifier with caching
- `TenantProvider`: Legacy service for tenant resolution (being phased out in favor of AsyncLocal pattern)
- `TenantContext`: Immutable data class holding tenant `Identifier` and `DatabaseName`

## Testing
- Unit tests for domain logic
- Integration tests for API endpoints
- Use the Test project naming convention: `{ModuleName}.Tests`

## Database
- Use Entity Framework Core with database-per-tenant architecture
- Implement proper migrations for each module
- Use configuration classes for entity mapping
- Follow repository pattern for data access
- Each module has its own schema (e.g., `Schemas.Users`, `Schemas.Customers`)
- Tenants module uses catalog database (`CatalogTenantDatabase`)
- Other modules use tenant-specific databases resolved through `TenantProvider`

## Error Handling
- Use global exception handling
- Return proper HTTP status codes
- Log errors appropriately using Serilog
- Use the Result pattern for business logic errors

## Background Jobs and Scheduling

### Quartz.NET Integration
- Use Quartz.NET for scheduled background tasks
- Jobs are tenant-aware and process all tenants
- Configured through `ConfigureOptions<ConfigureProcessOutboxJob>` pattern
- **CRITICAL**: All `ProcessOutboxJob` classes must be decorated with `[DisallowConcurrentExecution]` attribute to prevent race conditions and ensure proper clustering support

### Outbox Pattern Implementation
- Each module has its own outbox processing job
- Jobs retrieve tenant list from gRPC service
- Process outbox messages per tenant with proper tenant context
- Cached tenant information for performance
- Each module defines a module-specific outbox repository interface (e.g., `IOutboxMessage{ModuleName}Repository`)

### Job Implementation Pattern

**IMPORTANT**: Background jobs that need access to tenant-specific DbContext must use `IServiceProvider` to resolve dependencies **inside** the tenant scope, not in the constructor. This ensures the DbContext is created after the tenant context is established.

**Module-Specific Outbox Repository Pattern:**
Each module should define its own outbox repository interface that inherits from the common `IOutboxMessageRepository`:

```csharp
// Application Layer: ModularMonolithEventDriven.Modules.{ModuleName}.Application.Outbox.IOutboxMessage{ModuleName}Repository
public interface IOutboxMessage{ModuleName}Repository : Common.Application.Outbox.IOutboxMessageRepository
{
}

// Infrastructure Layer: ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure.Outbox.OutboxMessage{ModuleName}Repository
public class OutboxMessage{ModuleName}Repository : OutboxMessageRepository<{ModuleName}DbContext>, IOutboxMessage{ModuleName}Repository
{
    public OutboxMessage{ModuleName}Repository({ModuleName}DbContext dbContext) : base(dbContext)
    {
    }
}
```

**ProcessOutboxJob Implementation:**

```csharp
[DisallowConcurrentExecution]  // REQUIRED for Quartz.NET clustering
internal sealed class ProcessOutboxJob(
    ILogger<ProcessOutboxJob> logger,
    ITenantContextInitializer tenantContextInitializer,
    ICacheService cacheService,
    TenantGrpcService.TenantGrpcServiceClient tenantGrpcServiceClient,
    IServiceProvider serviceProvider) : IJob  // Use IServiceProvider, not DbContext directly
{
    private const string ModuleName = "ModuleName";
    private const string cacheKey = Common.Application.Caching.CacheKeys.Tenants.TenantIdentifiers;

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("{Module} - Beginning to process outbox messages", ModuleName);

        // Get all tenant identifiers (cached)
        IList<string>? tenantIdentifiers = await cacheService.GetAsync<IList<string>>(cacheKey);
        
        if (tenantIdentifiers is null || tenantIdentifiers.Count == 0)
        {
            var request = new Empty();
            try
            {
                TenantListGrpcResponse tenantListResponse = await tenantGrpcServiceClient.GetAllTenantInfoAsync(request);
                tenantIdentifiers = [.. tenantListResponse.Tenants.Select(tenant => tenant.Identifier)];
            }
            catch (Grpc.Core.RpcException ex)
                when (ex.StatusCode == Grpc.Core.StatusCode.NotFound || ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
            {
                logger.LogError(ex, "{Module} - Exception while reading the tenantList", ModuleName);
                logger.LogInformation("{Module} - No tenants found to process outbox messages", ModuleName);
                return;
            }
            await cacheService.SetAsync(cacheKey, tenantIdentifiers, TimeSpan.FromDays(1));
        }
        
        foreach (var tenantIdentifier in tenantIdentifiers)
        {
            logger.LogInformation("{Module} - Processing outbox messages for tenant {TenantId}", ModuleName, tenantIdentifier);
            try
            {
                // Set tenant context and resolve tenant-specific dependencies inside the scope
                await tenantContextInitializer.UseTenantAsync(tenantIdentifier, async () =>
                {
                    // Create a scope to resolve tenant-specific dependencies AFTER tenant context is set
                    await using var scope = serviceProvider.CreateAsyncScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<I{ModuleName}UnitOfWork>();
                    var outboxMessageRepository = scope.ServiceProvider.GetRequiredService<IOutboxMessage{ModuleName}Repository>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
                    
                    await ProcessOutboxMessages(context, unitOfWork, outboxMessageRepository, publisher);
                });
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "{Module} - Exception while processing outbox messages for tenant {TenantId}",
                    ModuleName,
                    tenantIdentifier);
            }
        }
        
        logger.LogInformation("{Module} - Completed processing outbox messages", ModuleName);
    }

    private async Task ProcessOutboxMessages(
        IJobExecutionContext context,
        I{ModuleName}UnitOfWork unitOfWork,
        IOutboxMessage{ModuleName}Repository outboxMessageRepository,
        IPublisher publisher)
    {
        List<OutboxMessage> outboxMessages = await outboxMessageRepository
            .GetUnprocessedAsync(context.CancellationToken);

        if (outboxMessages.Count == 0)
        {
            return;
        }

        foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            try
            {
                IDomainEvent? domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    outboxMessage.Content, SerializerSettings.Instance);
                if (domainEvent is null)
                {
                    continue;
                }
                
                await publisher.Publish(domainEvent, context.CancellationToken);
                
                outboxMessage.MarkSuccess();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "{Module} - Exception while processing outbox message {MessageId}",
                    ModuleName,
                    outboxMessage.Id);

                outboxMessage.MarkError(ex.Message);
            }
        }

        outboxMessageRepository.UpdateRange(outboxMessages);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
```

**Module Registration:**
```csharp
// In {ModuleName}Module.cs
services.AddScoped<Application.Outbox.IOutboxMessage{ModuleName}Repository, OutboxMessage{ModuleName}Repository>();
```

**Why This Pattern is Required:**
- DbContext requires `ITenantContextAccessor.Current` to resolve the tenant-specific connection string
- At job construction time, no tenant context exists yet
- Tenant context is only established inside `ITenantContextInitializer.UseTenantAsync()`
- By using `IServiceProvider` and creating a scope inside the tenant context callback, dependencies are resolved with the correct tenant context
- This pattern applies to ANY dependency that directly or indirectly depends on DbContext (repositories, unit of work, etc.)
- Module-specific repository interfaces allow for better type safety and module isolation

## Security
- Implement proper authentication and authorization
- Validate all inputs
- Use HTTPS in production
- Follow OWASP guidelines

## Logging
- Use structured logging with Serilog
- Include correlation IDs for tracing
- Log at appropriate levels (Debug, Info, Warning, Error)
- Don't log sensitive information

When generating code, ensure it follows these patterns and integrates well with the existing modular monolith architecture. This architecture emphasizes complete tenant isolation through database-per-tenant design, robust multi-tenancy support, and clean separation of concerns across modules.
