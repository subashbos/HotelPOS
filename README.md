# 🏨 HotelPOS - Advanced Billing & Management System

Welcome to **HotelPOS**, a professional-grade Point of Sale (POS) and inventory management system built with **.NET 10** and **WPF**, specifically optimized for enterprise hospitality businesses.

---

## 📚 Architectural & Technical Documentation

For in-depth explanations of the system's design and features, refer to the dedicated guides:

1. **[User Guide & Features](file:///d:/HotelPOS/ProjectDocumentation.md)**  
   *Operational manual for cashiers, business owners, GST setups, and POS shortcuts.*
2. **[System Design & Architecture](file:///d:/HotelPOS/SystemDesign.md)**  
   *Comprehensive layout of the Clean Architecture implementation and design patterns (CQRS, MediatR).*
3. **[Technical Reference & Deep Dives](file:///d:/HotelPOS/TechnicalReference.md)**  
   *Detailed documentation of thread safety, printing, tax calculation engines, and database synchronization.*

---

## 🚀 Key Architectural Strengths

### 🏛️ Clean Architecture & Scalability
- **Domain-Driven Design (DDD)**: Business entities and rules are kept completely decoupled from outer persistence layers.
- **Refactored DTO Organization**: Organized all request/response models into modular domain namespaces under `HotelPOS.Application/DTOs/` (Item, Order, Table, Report), separating application logic from serialization structures.
- **Repository Pattern**: Strict decoupling of data persistence layers (`HotelPOS.Persistence`) using Entity Framework Core.

### 💳 Advanced Sales & Billing Operations
- **Split & Partial Payments**: Supports accepting multi-mode payments (Cash, Card, UPI) on the same transaction and tracking outstanding balances.
- **Refunds & Voids**: Proportional and item-level return calculations with stock replenishment, and manager-authorized transaction voids.

### 📊 Reporting & Business Intelligence
- **Profit Margins**: Real-time margin percentages, food cost trends, and low-margin warnings.
- **Wastage Tracking**: Logs ingredient and stock wastage (Spoilage, Damage, Overproduction) with automated fallback cost valuations.
- **Predictive Alerts**: High-precision low stock notification system with estimated stock depletion timelines.

### 💾 Disaster Recovery
- **Replication**: Configurable automated database backup replication to off-site directories.
- **One-Click Restore**: Automatic restoration of SQLite and SQL Server databases directly from the Settings UI.

### 🛡️ Enterprise-Grade Thread Safety
- **Scoped DbContext Resolution**: Avoids concurrent EF Core access exceptions without relying on global blocks by utilizing short-lived `IServiceScope` lifecycles on distinct UI execution contexts, ensuring independent database contexts per operation.
- **Safe Cart Synchronization**: Core billing operations (`CartService`) utilize robust concurrency models to guarantee thread safety during fast checkout operations.

### 🧪 Robust Test Coverage
- **100% Green Suite**: Anchored by **593 comprehensive integration and unit tests** covering checkout calculations, tax validations, cashier shifts, and billing edge cases.

---

## 🛠️ Technology Stack
- **Presentation**: Windows Presentation Foundation (WPF) with XAML & CommunityToolkit MVVM
- **Runtime & Orchestration**: .NET 10, MediatR (CQRS Pattern)
- **Database / ORM**: Entity Framework Core 10 (Microsoft SQL Server / SQLite)
- **Compliance**: Indian GST engine (Tax Invoice & Bill of Supply modes ready)
- **Exporting**: ClosedXML for premium Excel report generation

---

## 🛠️ How to Build

### Prerequisites
- **IDEs**: [Visual Studio 2022](https://visualstudio.microsoft.com/) (version 17.10 or higher with the **.NET Desktop Development** workload) or JetBrains Rider.
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- **Database**: SQL Server Express / LocalDB (or SQLite as a fallback or configured option).

### Build via Command Line
Run the following commands in the root of the project repository:
```bash
# Restore dependencies
dotnet restore

# Build the solution in Release configuration
dotnet build --configuration Release
```

### Build via Visual Studio
1. Open the solution file `HotelPOS.sln` or `HotelPOS/HotelPOS.slnx` in Visual Studio.
2. Select the **Release** configuration and **Any CPU** platform.
3. Right-click the solution in Solution Explorer and click **Build Solution**.

---

## 📖 How to Use

### Database Initialization
On the very first launch, the system automatically bootstraps the database schema (SQL Server / SQLite) and applies necessary column/table migrations programmatically (e.g., adding `CostPrice` and `MinStockThreshold` columns if missing), so no manual script execution is required.

### 1. Launching & Logging In
1. Set the **HotelPOS** project as the startup project.
2. Press `F5` to start debugging or `Ctrl+F5` to run.
3. Log in with the configured cashier or administrator credentials (standard credentials are provided upon setup).

### 2. Standard Billing Workflow (Keyboard Optimized)
- **Search Item**: Press `F1` or `F3` to focus the search box. Type the item name or scan a barcode.
- **Add to Cart**: Press `Enter` to add the selected item to the cart list.
- **Adjust Quantities**: Use the `+` or `-` keyboard shortcuts or click directly on the quantity cells in the grid to edit in-place.
- **Checkout**: Press `F4` to instantly bring up the checkout dialog, choose payment options, and print the receipt.

### 3. Business Intelligence & Wastage Logging
- Navigate to the **BI Analytics** tab.
- Switch to the **Wastage Tracking** sub-tab.
- Select the item, enter the quantity wasted, choose the wastage reason (e.g., *Spoilage*, *Damage*), and click **Log Wastage Entry**. If the item's raw cost price is not configured, the system automatically falls back to its base selling price to compute the lost value.

---

## 📦 Developer Utilities
All active diagnostic tools and developer scripts have been cleanly organized under the `/tools` directory:
- `tools/Database/check_db.cs`: Safe DB schema validation diagnostic tool.
