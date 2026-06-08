# HotelPOS Technical Reference

This document provides deep-dive implementation details for the core technical components, database structures, and synchronization engines of the HotelPOS system.

---

## 1. DbContext Concurrency Synchronization (Scoped DbContext Resolution)
To allow the WPF client to perform asynchronous database queries without throwing concurrency errors, the application uses short-lived dependency injection scopes.

### Technical Design
- **Engine**: Dynamic scope factory (`App.CreateDbScope()`) that spawns an `IServiceScope` from the root ServiceProvider container.
- **Scope Isolation**: Limits the lifetime of resolved services and `DbContext` to the boundary of a single asynchronous operation or logical transaction block, ensuring that distinct database connections are utilized for concurrent operations.
- **Fallback**: Includes `System.Windows.Application.Current` check. During xUnit unit test runs, `App.CreateDbScope()` returns a `DummyScope` that resolves services to `null`. This forces components to seamlessly fall back to their constructor-injected fields (mocked by tests).

```csharp
using (var scope = App.CreateDbScope())
{
    var orderService = scope.ServiceProvider.GetService<IOrderService>() ?? _orderService;
    await orderService.SaveOrderAsync(items, table);
}
```

---

## 2. CartState Stateful Engine (`CartService`)
The stateful core of the billing POS workspace, managing active tables and customer bills in memory.

### Technical Specifications
- **Concurrency Model**: Utilizes `ConcurrentDictionary<int, List<OrderItem>>` mapping Table Numbers (keys) to active order rows (values).
- **Mutations & Thread Safety**: All collection manipulations (additions, quantity changes, updates) are strictly enclosed within private `lock (_lock)` blocks.
- **Key Workflows**:
  - `TransferTable(sourceTable, targetTable)`: Atomically merges or re-assigns cart list keys.
  - `HoldOrder(table, cashier)`: Converts the cart collections into persistent KOT entities, clearing active memory states.

---

## 3. Clean Architecture DTO Contracts
All data validation contracts and request models are separated from outer persistent ORM schemas and UI services.
- **Namespaces**: Divided into clean folders under `HotelPOS.Application/DTOs/` (e.g. `Item/CreateItemDto`, `Table/CreateTableDto`, `Order/CreateOrderDto`).
- **Data Hydration**: Controllers, handlers, and MVVM ViewModels instantiate these immutable structures to map incoming payloads without exposing database entity objects.

---

## 4. UI/UX Theming & Dynamic Assets
The desktop interface supports instant dark/light themes.
- **Resource Management**: Dynamic dictionaries are defined in `/Themes/DarkTheme.xaml` and `/Themes/LightTheme.xaml`.
- **Runtime Swapping**: `ThemeService` clears the first index of `MergedDictionaries` and injects the selected theme URI dynamically.
- **Micro-Animations**: Uses hardware-accelerated WPF `DoubleAnimation` for smooth sidebar collapses and tab transitions.

---

## 5. Persistence & Migrations (EF Core 10)
Database synchronization and history tracking are automated on application initialization.

### Schema Safeguards
- **Baseline Migration Strategy**: To prevent database collision crashes (such as `Table Already Exists`), `App.xaml.cs` inspects database states at startup.
- **Migrations History Injection**: If physical tables are already present on disk, migration history markers are baselined dynamically via `ExecuteSqlRaw` before running `context.Database.Migrate()`.
- **Transaction Preservations**: Line item histories (`OrderItem`) snapshot item pricing, CGST, and SGST rates at the precise moment of sale, ensuring total historical audit accuracy.
