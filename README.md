# HotelPOS

HotelPOS is a Windows desktop point-of-sale application for hotel/restaurant billing, built with WPF and .NET.

## Tech Stack

- .NET `10.0` (`net10.0-windows`)
- WPF desktop UI
- Entity Framework Core + SQL Server
- xUnit + Moq for tests
- Serilog file logging

## Solution Structure

The solution file is located at `HotelPOS/HotelPOS.slnx`.

- `HotelPOS/` - WPF UI application (startup project)
- `HotelPOS.Domain/` - domain models and core contracts
- `HotelPOS.Application/` - application services and interfaces
- `HotelPOS.Persistence/` - EF Core persistence layer
- `HotelPOS.Infrastructure/` - cross-cutting/infrastructure services
- `HotelPOS.Tests/` - automated unit tests

## Prerequisites

- Windows 10/11
- .NET SDK 10.0 or later
- SQL Server (local or remote)

## Configuration

Main app configuration is in `HotelPOS/appsettings.json`.

Update the connection string before running:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=HotelWPF;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## Build and Run

From the repository root:

```powershell
dotnet restore .\HotelPOS\HotelPOS.slnx
dotnet build .\HotelPOS\HotelPOS.slnx
dotnet run --project .\HotelPOS\HotelPOS.csproj
```

## Run Tests

```powershell
dotnet test .\HotelPOS.Tests\HotelPOS.Tests.csproj
```

## Logging

Application logs are written to rolling files under:

- `HotelPOS/bin/<Configuration>/net10.0-windows/logs/`

Example log file pattern:

- `pos-log-YYYYMMDD.txt`

## Notes

- `appsettings.json` is copied to the output directory on build.
- The app uses dependency injection for services, repositories, and views.
- Keep sensitive production connection strings out of source control.

## Documentation

- Knowledge transfer handbook: `docs/KNOWLEDGE_TRANSFER.md`

