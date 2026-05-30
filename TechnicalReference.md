# HotelPOS Technical Reference

This document provides deep-dive implementation details for the core technical components, database structures, and synchronization engines of the HotelPOS system.

---

## 1. DbContext Concurrency Synchronization (`App.DbLock`)
To allow a shared scoped Entity Framework DbContext instance to handle multi-threaded queries without throws, a global semaphore lock is integrated across the WPF codebase.

### Technical Design
- **Engine**: System `SemaphoreSlim(1, 1)` declared statically on `App` namespace.
- **Lock Scope**: Limits database reads and writes to a single concurrent operation, serializing access safely.
- **Implementation Pattern**: Strict `await App.DbLock.WaitAsync()` followed by database execution within a `try` block, and a guaranteed `App.DbLock.Release()` inside `finally`.
- **Preclusion of Deadlocks**: Semaphore calls inside view event handlers (e.g. `CategoryView`, `TableView`) are structured to release locks *before* subsequent view refreshing operations re-acquire them.

```csharp
await App.DbLock.WaitAsync();
try
{
    // Scoped DB Operation
    await _orderService.SaveOrderAsync(items, table);
}
finally
{
    App.DbLock.Release();
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
