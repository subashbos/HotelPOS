# HotelPOS - Professional Billing & Management System

## 1. Introduction
HotelPOS is a modern, high-performance Point of Sale (POS) and billing application optimized specifically for hotels, restaurants, and hospitality businesses. Built on **.NET 10** and **WPF**, it provides a lightning-fast, keyboard-driven, thread-safe user experience for rapid billing, kitchen management, and compliance-ready tax reporting.

---

## 2. Key Features

### 🛒 Modern Billing Workflow
- **Active Tabs**: Move seamlessly between multiple active tables or customers using the top navigation bar.
- **+ New Bill**: Instantly start a billing session with a visual dining table layout selector.
- **Intelligent Search**: Find items by name or barcode with real-time stock and inventory validation.
- **In-Grid Editing**: Update quantities and prices directly in the active bill table for maximum cashier efficiency.

### 🍱 Table & Kitchen Management
- **Table Transfers**: Move items from one table to another or merge bills atomically.
- **KOT (Kitchen Order Ticket)**: Hold orders and automatically print instruction tickets for the kitchen.
- **Table Status**: Visual color-coded indicators for Occupied, Current, and Available tables.

### 🇮🇳 Indian GST Compliance
- **Dual Scheme Support**: Supports both **Regular GST** (Tax Invoices with detailed breakdowns) and **Composition Scheme** (Bill of Supply).
- **Auto-Title**: Automatically switches invoice headers based on legal compliance settings.
- **Rounding**: Configurable round-off logic to the nearest rupee.

---

## 3. User Guide

### 3.1 Creating a Bill
1. Press **F1 / F3** to focus the Search Box.
2. Type the item name or scan a barcode.
3. Press **Enter** to add the item to the cart.
4. Adjust quantities using the **+/-** buttons or by typing directly in the QTY column.
5. Click **Checkout** (or press **F4**) to finalize the order.

### 3.2 Moving a Table
1. In the "Current Bill" panel, click **Move Items**.
2. Select the target table from the layout popup.
3. The items will be moved, and the old table will be cleared.

### 3.3 Configuring Settings
- Navigate to the **Settings** tab.
- **Hotel Profile**: Set your business name, address, phone number, and GSTIN.
- **Printer**: Select your default thermal printer and choose between **Thermal** or **A4** formats.
- **GST Scheme**: Toggle "GST Composition Scheme" if your business does not collect tax from customers.

---

## 4. Technical Setup

### Prerequisites
- Visual Studio 2022 (v17.10+)
- .NET 10 SDK
- Microsoft SQL Server (LocalDB or full instance).

### Building the Project
1. Open the modern `HotelPOS/HotelPOS.slnx` (or `HotelPOS.sln` at the root) in Visual Studio.
2. Ensure the solution configuration is set to `Debug` or `Release`.
3. Right-click the **HotelPOS** project and select **Set as Startup Project**.
4. Press **F5** or `Ctrl + F5` to run.

### Database Configuration
- The app uses **Microsoft SQL Server** for persistence.
- To switch databases or update server addresses, update the connection string in `appsettings.json`.

---

## 5. Keyboard Shortcuts
| Key | Action |
| :--- | :--- |
| **F1 / F3** | Focus Search Box |
| **F4** | Save & Print Bill (Checkout) |
| **Up / Down** | Navigate Auto-complete list |
| **Enter** | Select Item / Add to Cart |
| **Escape** | Close Popups / Clear Search |

---

## 6. Project Structure
For detailed architectural info, refer to [SystemDesign.md](SystemDesign.md).

- **HotelPOS.WPF**: The main UI (WPF/XAML) containing views and view-models.
- **HotelPOS.Domain**: Core data models and database entities.
- **HotelPOS.Application**: Business logic, DTOs, and service interfaces.
- **HotelPOS.Persistence**: Entity Framework DbContext, database migrations, and repositories.
- **HotelPOS.Infrastructure**: Core infrastructure implementations.
- **HotelPOS.Tests**: Comprehensive unit and integration test suite (504 tests).
