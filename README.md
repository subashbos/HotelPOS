# 🏨 HotelPOS

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Desktop-0078D7?style=flat-square&logo=windows)
![EF Core](https://img.shields.io/badge/EF_Core-SQL_Server-32B900?style=flat-square&logo=microsoft-sql-server)

HotelPOS is a robust, Windows desktop point-of-sale application designed specifically for hotel and restaurant billing. Built with WPF and modern .NET 10.0, it features a clean architecture, dependency injection, and a robust SQL Server backend.

## ✨ Features

- **Billing & Order Management**: Comprehensive point-of-sale interface.
- **Inventory Tracking**: Item management with category support and barcode scanning.
- **Taxation & Discounts**: GST handling (0%, 5%, 12%, 18%, 28%) and custom discounts.
- **Reporting & Ledger**: Daily cash sessions, date-wise reports, journal, and ledger views.
- **Receipt Printing**: Integrated thermal printer receipt generation.
- **Security & Auditing**: Role-based access control, soft-delete, and full audit logging.
- **Themes**: Support for Light and Dark modes.

---

## 🏗️ Solution Architecture

The project follows a Clean Architecture approach to separate concerns and maintain testability.

- `HotelPOS.slnx` - Main solution file.
- `HotelPOS/` *(UI Layer)* - WPF desktop application, ViewModels, and Views.
- `HotelPOS.Application/` *(Application Layer)* - Business logic, services, and interfaces.
- `HotelPOS.Domain/` *(Domain Layer)* - Core entities, models, and domain events.
- `HotelPOS.Persistence/` *(Persistence Layer)* - Entity Framework Core DbContext and repository implementations.
- `HotelPOS.Infrastructure/` *(Infrastructure Layer)* - Cross-cutting concerns (e.g., Logging, Backups, Notifications).
- `HotelPOS.Tests/` *(Test Layer)* - Automated unit and integration tests using xUnit and Moq.

---

## 🚀 Getting Started

### Prerequisites

- Windows 10 or Windows 11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- SQL Server (LocalDB or full SQL Server)
- Visual Studio 2022 (recommended)

### Configuration

The main application configuration is located in `HotelPOS/appsettings.json`.
Before running the application, ensure your connection string is pointing to your local SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HotelPOS;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Build & Run via CLI

From the repository root:

```powershell
# Restore dependencies
dotnet restore HotelPOS.slnx

# Build the solution
dotnet build HotelPOS.slnx

# Run the application
dotnet run --project HotelPOS\HotelPOS.csproj
```

### Run Tests

Execute the automated test suite to ensure system stability:

```powershell
dotnet test HotelPOS.Tests\HotelPOS.Tests.csproj
```

---

## 📜 Logging & Diagnostics

Application logs are handled by Serilog and are written to rolling files located in the output directory:

- Path: `HotelPOS/bin/<Configuration>/net10.0-windows/logs/`
- Pattern: `pos-log-YYYYMMDD.txt`

---

## 📚 Documentation

For deeper technical insights and a history of architectural decisions, please refer to the Knowledge Transfer handbook:
- 📖 [Knowledge Transfer Document](docs/KNOWLEDGE_TRANSFER.md)
