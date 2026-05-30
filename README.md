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

### 🛡️ Enterprise-Grade Thread Safety
- **Global DbContext Serialization**: Implements a centralized synchronization semaphore (`App.DbLock`) across **14 major views and view-models** to fully eliminate EF Core concurrent access exceptions (`InvalidOperationException`) in highly dynamic user interfaces.
- **Safe Cart Synchronization**: Core billing operations (`CartService`) utilize robust concurrency models to guarantee thread safety during fast checkout operations.

### 🧪 Robust Test Coverage
- **100% Green Suite**: Anchored by **504 comprehensive integration and unit tests** covering checkout calculations, tax validations, cashier shifts, and billing edge cases.

---

## 🛠️ Technology Stack
- **Presentation**: Windows Presentation Foundation (WPF) with XAML & CommunityToolkit MVVM
- **Runtime & Orchestration**: .NET 10, MediatR (CQRS Pattern)
- **Database / ORM**: Entity Framework Core 10 (Microsoft SQL Server)
- **Compliance**: Indian GST engine (Tax Invoice & Bill of Supply modes ready)
- **Exporting**: ClosedXML for premium Excel report generation

---

## ⚡ Quick Start
1. Open the solution in **Visual Studio 2022 (v17.10+)**:
   - For WPF Desktop development: Open `HotelPOS/HotelPOS.slnx` (Modern XML Solution format).
   - For full stack development: Open `HotelPOS.sln` (Root).
2. Restore and Build the solution.
3. Run **HotelPOS**.
4. **Login**: Use standard credentials (or configure cashiers under *Settings > Users*).
5. **Fast Billing**: Use keyboard shortcuts:
   - `F1` or `F3`: Focus menu item search
   - `F4`: Instantly trigger checkout & generate print invoice

---

## 📦 Developer Utilities
All active diagnostic tools and developer scripts have been cleanly organized under the `/tools` directory:
- `tools/Database/check_db.cs`: Safe DB schema validation diagnostic tool.
