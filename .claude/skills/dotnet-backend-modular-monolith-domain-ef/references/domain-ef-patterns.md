# Domain Implementation, EF Configuration & Migrations — Complete Reference

---

## 1. Domain Entity Patterns

### Base class hierarchy (from `Common.Domain.Primitives`)

```
Entity<TKey>                 → Id, DomainEvents, equality by Id
  └── AuditableEntity<TKey>  → + CreatedAt, UpdatedAt (auto-set by BaseDbContext)
        └── AuditableGuidEntity  → shorthand: AuditableEntity<Guid>
```

### Decision guide

| Scenario | Base class |
|---|---|
| Aggregate root (has its own lifecycle, owns child entities) | `AuditableGuidEntity` |
| Child entity (owned by an aggregate, no independent lifecycle) | `Entity<Guid>` |
| Lookup / reference data (non-Guid key) | `AuditableEntity<TKey>` |
| Immutable grouping with no identity | C# `record` (value object) |

---

### Aggregate root — full pattern

```csharp
// Domain/Order.cs
public sealed class Order : AuditableGuidEntity
{
    private readonly List<OrderItem> _items = [];

    private Order() { }  // ← required: EF needs a parameterless constructor

    private Order(Guid id, string customerId, string customerEmail, List<OrderItem> items) : base(id)
    {
        CustomerId = customerId;
        CustomerEmail = customerEmail;
        _items = items;
        Status = OrderStatus.Pending;
        TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity);

        RaiseDomainEvent(new OrderCreatedDomainEvent(id, customerId));  // optional
    }

    // Properties — private setters, never mutated from outside
    public string CustomerId { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? FailureReason { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // Factory method — single validated entry point
    public static Order Create(Guid id, string customerId, string customerEmail, List<OrderItem> items)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerId);
        ArgumentException.ThrowIfNullOrEmpty(customerEmail);
        if (items.Count == 0)
            throw new ArgumentException("Order must have at least one item.", nameof(items));
        return new Order(id, customerId, customerEmail, items);
    }

    // Behaviour methods — express domain language, not setters
    public void MarkAsStockReserved() => Status = OrderStatus.StockReserved;
    public void MarkAsCompleted() => Status = OrderStatus.Completed;
    public void MarkAsFailed(string reason) { Status = OrderStatus.Failed; FailureReason = reason; }
    public void MarkAsCancelled() => Status = OrderStatus.Cancelled;
}
```

**Rules:**
- Always `sealed class`
- Private `_field` for collections — expose as `IReadOnlyList<T>`
- `private Entity() { }` — EF Core requirement
- `static Create(...)` validates before constructing
- Behaviour methods use ubiquitous language (`MarkAsCancelled`, `ReserveStock`), never `SetStatus`

---

### Child entity — owned by aggregate

```csharp
// Domain/OrderItem.cs
public sealed class OrderItem : Entity<Guid>
{
    private OrderItem() { }

    public OrderItem(Guid id, Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
}
```

- Use `Entity<Guid>` (no audit timestamps — child's lifecycle is tied to the root)
- No factory method needed — created only by the aggregate root

---

### Value object (owned / embedded)

A value object has no identity. Use a C# `record` and map with `OwnsOne` or `OwnsMany` in EF.

```csharp
// Domain/ReservationItem.cs  (owned by StockReservation)
public sealed record ReservationItem(Guid ProductId, int Quantity);
```

EF configuration: see `OwnsMany` example in section 3.

---

### Enum

Define enums in the same file as the entity that uses them (unless shared across entities).

```csharp
// Domain/OrderStatus.cs
public enum OrderStatus
{
    Pending = 0,
    StockReserved = 1,
    PaymentProcessed = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
```

- Always assign explicit integer values — prevents migration drift if values are reordered
- Persist as `string` via `.HasConversion<string>()` — human-readable in the DB and migration-safe

---

### Domain event

```csharp
// Domain/Events/OrderCreatedDomainEvent.cs
public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    string CustomerId) : DomainEvent;
// DomainEvent base provides: EventId (Guid), OccurredOn (DateTime)
```

Raise inside the entity:
```csharp
RaiseDomainEvent(new OrderCreatedDomainEvent(id, customerId));
```

Events accumulate on `entity.DomainEvents` (from `Entity<TKey>`). Wire up dispatch in `BaseDbContext.SaveChangesAsync` if intra-module handling is needed.

---

## 2. EF Entity Configuration in `OnModelCreating`

Configuration lives **inline** inside the module's `DbContext.OnModelCreating` — not in separate `IEntityTypeConfiguration<T>` classes. This keeps all persistence knowledge in one file per module.

### String properties

```csharp
b.Property(p => p.Name).IsRequired().HasMaxLength(200);
b.Property(p => p.Sku).IsRequired().HasMaxLength(50);
b.Property(p => p.FailureReason).HasMaxLength(500);       // nullable — no IsRequired()
b.Property(p => p.Notes).HasMaxLength(1000);
```

**Conventions:**
| Field type | Max length |
|---|---|
| Name / Title | 200 |
| Code / SKU / Slug | 50 |
| Email | 200 |
| Id / short reference | 100 |
| Reason / message | 500 |
| Long message / body | 1000 |

---

### Decimal precision

Always set precision — SQL Server default is `decimal(18,2)`:
```csharp
b.Property(p => p.Price).HasPrecision(18, 2);
b.Property(p => p.Amount).HasPrecision(18, 2);
b.Property(p => p.TotalAmount).HasPrecision(18, 2);
```

---

### Enum → string conversion

```csharp
b.Property(o => o.Status).HasConversion<string>().IsRequired();
```

Stores `"Pending"`, `"Completed"` etc. instead of integers. Safe when values are added later.

---

### Unique index

```csharp
b.HasIndex(p => p.Sku).IsUnique();
b.HasIndex(p => p.OrderId).IsUnique();   // one payment per order
```

---

### Non-unique index

```csharp
b.HasIndex(i => i.OrderId);   // foreign key navigation — improves join performance
```

---

### One-to-many relationship (child entity with FK)

```csharp
// On the aggregate root (Order):
b.HasMany(o => o.Items)
 .WithOne()
 .HasForeignKey(i => i.OrderId)
 .OnDelete(DeleteBehavior.Cascade);
```

The child entity (`OrderItem`) does **not** need a navigation property back to the root.

---

### Owned entity — `OwnsMany` (value object collection)

Use when a value object has no separate lifecycle and is always loaded with its owner.

```csharp
// StockReservation owns a collection of ReservationItem records
modelBuilder.Entity<StockReservation>(b =>
{
    b.ToTable("StockReservations");
    b.HasKey(r => r.Id);
    b.Property(r => r.Status).HasConversion<string>();
    b.OwnsMany(r => r.Items, ib =>
    {
        ib.ToTable("StockReservationItems");
        ib.WithOwner().HasForeignKey("ReservationId");
        ib.Property(i => i.ProductId);
        ib.Property(i => i.Quantity);
    });
});
```

- `WithOwner().HasForeignKey("ReservationId")` — shadow property FK, not on the record
- EF generates a composite PK `(ReservationId, Id)` with an auto-increment shadow column

---

### `OwnsOne` (single value object embedded)

```csharp
b.OwnsOne(o => o.Address, ab =>
{
    ab.Property(a => a.Street).IsRequired().HasMaxLength(200);
    ab.Property(a => a.City).IsRequired().HasMaxLength(100);
    ab.Property(a => a.PostalCode).HasMaxLength(20);
});
// Stored as columns on the owner's table (no separate table)
```

---

## 3. DbContext Structure

```csharp
// Infrastructure/Persistence/InventoryDbContext.cs
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : BaseDbContext(options), IInventoryUnitOfWork   // ← implements module-specific UoW
{
    // One DbSet per aggregate root — never for child/owned entities
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // ← must call base (sets audit timestamps)
        modelBuilder.HasDefaultSchema("inventory");   // ← schema-per-module

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            b.Property(p => p.Price).HasPrecision(18, 2);
            b.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<StockReservation>(b =>
        {
            b.ToTable("StockReservations");
            b.HasKey(r => r.Id);
            b.Property(r => r.Status).HasConversion<string>();
            b.OwnsMany(r => r.Items, ib =>
            {
                ib.ToTable("StockReservationItems");
                ib.WithOwner().HasForeignKey("ReservationId");
                ib.Property(i => i.ProductId);
                ib.Property(i => i.Quantity);
            });
        });
    }
}
```

**Rules:**
- `sealed class` — not inherited
- Primary constructor `(DbContextOptions<TContext> options)` — passed straight to base
- Implements `I<Module>UnitOfWork` (except Notifications, which has no UoW interface)
- `HasDefaultSchema` — always set; value = lowercase module name
- `base.OnModelCreating(modelBuilder)` — always call first
- No `DbSet` for child/owned entities — they are reached via the aggregate root

---

## 4. Design-Time Factory

Required so `dotnet ef migrations add` and `dotnet ef database update` work from the `Infrastructure` project (which has no `Program.cs`).

```csharp
// Infrastructure/Persistence/InventoryDbContextFactory.cs
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("ModularMonolithEventDrivenDb")
            ?? throw new InvalidOperationException("Connection string 'ModularMonolithEventDrivenDb' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSqlServer(connectionString,
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "inventory"));
        //                                                           ↑ schema-specific migration history

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
```

**Critical:** `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")` — each module stores its migration history in its own schema so migrations don't collide.

The `appsettings.json` must exist in the directory where `dotnet ef` is run (i.e., the `Infrastructure` project folder). It reads `ModularMonolithEventDrivenDb` connection string.

---

## 5. EF Migrations

### Add a new migration

Run from the repo root:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/<Module>/ModularMonolithEventDriven.Modules.<Module>.Infrastructure \
  --startup-project src/Modules/<Module>/ModularMonolithEventDriven.Modules.<Module>.Infrastructure \
  --context <Module>DbContext
```

Example — add a migration to Inventory:
```bash
dotnet ef migrations add AddWarehouseLocation \
  --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure \
  --startup-project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure \
  --context InventoryDbContext
```

Migrations land in: `Infrastructure/Persistence/Migrations/`

---

### Apply manually

```bash
dotnet ef database update \
  --project src/Modules/<Module>/ModularMonolithEventDriven.Modules.<Module>.Infrastructure \
  --startup-project src/Modules/<Module>/ModularMonolithEventDriven.Modules.<Module>.Infrastructure \
  --context <Module>DbContext
```

**Note:** The app also applies migrations automatically on startup via `Database.MigrateAsync()` in `Program.cs`. Manual apply is only needed when testing migrations without starting the API.

---

### Rollback to a previous migration

```bash
dotnet ef database update <PreviousMigrationName> \
  --project ... --startup-project ... --context <Module>DbContext
```

---

### Remove last (unapplied) migration

```bash
dotnet ef migrations remove \
  --project ... --startup-project ... --context <Module>DbContext
```

Only works if the migration has **not** been applied to the database yet.

---

### Inspect generated SQL

```bash
dotnet ef migrations script \
  --project ... --startup-project ... --context <Module>DbContext
```

---

## 6. What Belongs Where — Quick Reference

| Concern | Layer | File |
|---|---|---|
| Entity definition, factory, behaviour | Domain | `Domain/<EntityName>.cs` |
| Domain event definition | Domain | `Domain/Events/<EventName>DomainEvent.cs` |
| Enum definition | Domain | `Domain/<EntityName>.cs` or `Domain/<EnumName>.cs` |
| Error definitions | Domain | `Domain/Errors/<Module>Errors.cs` |
| Repository interface | Domain | `Domain/I<EntityName>Repository.cs` |
| EF configuration | Infrastructure | `Infrastructure/Persistence/<Module>DbContext.cs` |
| Repository implementation | Infrastructure | `Infrastructure/Persistence/<EntityName>Repository.cs` |
| Design-time factory | Infrastructure | `Infrastructure/Persistence/<Module>DbContextFactory.cs` |
| Migrations | Infrastructure | `Infrastructure/Persistence/Migrations/` |

---

## 7. Common Mistakes & Gotchas

| Mistake | Correct approach |
|---|---|
| Forgetting `private Entity() { }` | EF requires a parameterless constructor on every mapped entity |
| `DbSet` for a child entity (e.g. `OrderItem`) | Only aggregate roots get `DbSet`; children are reached via root |
| Missing `base.OnModelCreating(modelBuilder)` | `BaseDbContext` sets up audit timestamps — always call base first |
| Storing enum as integer | Use `.HasConversion<string>()` — integer values break if enum is reordered |
| Missing `HasPrecision` on `decimal` | EF warns and uses DB default; always set `(18, 2)` explicitly |
| Migrations history collision across modules | Each factory must set `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")` |
| Running `dotnet ef` from wrong directory | Always use `--project` and `--startup-project` pointing to the `Infrastructure` project |
| Setting properties from outside the entity | All business state changes go through behaviour methods; no public setters |
