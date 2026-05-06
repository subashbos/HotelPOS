# HotelPOS Technical Reference

This document provides deep-dive implementation details for the core technical components of the HotelPOS system.

---

## 1. CartService Implementation
The `CartService` is the stateful engine of the billing workflow. It maintains active sessions in memory using a `ConcurrentDictionary<int, List<OrderItem>>`.

### Key Methods
- `GetItems(int tableNumber)`: Returns a sorted list of items. It uses `.OrderBy(x => x.ItemName)` to ensure a consistent visual order in the UI.
- `TransferTable(source, target)`: Atomically moves items between tables. If the target table is occupied, items are merged; otherwise, the session is moved entirely.
- `HoldOrder(table, name)`: Captures the current cart state into a `HeldOrder` object, removes it from the active table, and triggers the KOT (Kitchen Order Ticket) workflow.

### Thread Safety
All collection mutations are wrapped in a private `lock (_lock)` object to prevent race conditions during rapid keyboard entry or multi-user scenarios.

---

## 2. BillingViewModel Logic
The `BillingViewModel` acts as the orchestrator for the POS UI.

### Data Synchronization (`UpdateCart`)
The `UpdateCart` method is called whenever the state changes (table switch, item added, etc.).
1. It retrieves the latest item list from `CartService`.
2. It reconciles the `ObservableCollection<CartRow>` by removing stale rows and updating existing ones.
3. **Sorting & Sequencing**: It explicitly sorts the collection alphabetically and reassigns `S.No` (Serial Number) from 1 to ensure a predictable UI experience.

### Keyboard Event Handling
Intercepted via `PreviewKeyDown` in `BillingView.xaml.cs`, commands are routed to the VM:
- **F4**: Executes `SaveOrderCommand`.
- **Enter (on Search)**: Adds the selected autocomplete item to the cart.
- **Enter (outside Search)**: Triggers checkout if the cart is not empty.

---

## 3. Receipt Generation (`ReceiptGenerator`)
Receipts are built using WPF **FlowDocuments** for high-resolution printing.

### Formatting Logic
- **Thermal Mode**: Sets `MaxPageWidth` to 285px and uses smaller font sizes (9pt - 15pt).
- **Compliance Toggles**: 
    - If `IsCompositionScheme` is true:
        - Header = `BILL OF SUPPLY`
        - Subtotal and GST lines are omitted.
    - If false:
        - Header = `TAX INVOICE`
        - Includes CGST/SGST breakdown per tax rate.

---

## 4. Database Schema (EF Core)
Persistence is handled via Entity Framework Core.

### Core Entities
- **Order**: Stores header info (ID, Date, Total, PaymentMode, Customer info).
- **OrderItem**: Stores line-item snapshots (Price, Tax %, Quantity) at the time of sale to preserve historical accuracy even if master item prices change.
- **SystemSetting**: A single-row table storing business profile and printer preferences.

### Migration Strategy
Standard EF Core Migrations are stored in `HotelPOS.Persistence`. The app automatically applies pending migrations on startup in `App.xaml.cs`.

---

## 5. Theming System
Themes are defined in `Themes/DarkTheme.xaml` and `Themes/LightTheme.xaml`.
- **Dynamic Swapping**: The `ThemeService` clears the `MergedDictionaries[0]` and inserts the new theme URI.
- **Color Tokens**: All UI elements bind to dynamic resources like `{DynamicResource PrimaryBrush}` rather than static colors.
