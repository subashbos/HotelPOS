# HotelPOS - Professional Billing & Management System

## 1. Introduction
HotelPOS is a modern, high-performance Point of Sale (POS) application designed specifically for hotels and restaurants. Built on .NET 10 and WPF, it provides a browser-like, keyboard-driven experience for rapid billing, kitchen management, and compliance-ready reporting.

---

## 2. Key Features

### 🛒 Modern Billing Workflow
- **Active Tabs**: Move between multiple active tables or customers using the top navigation bar.
- **+ New Bill**: Instantly start a new session with a visual table-layout selector.
- **Intelligent Search**: Find items by name or barcode with real-time stock validation.
- **In-Grid Editing**: Update quantities and prices directly in the billing table for maximum speed.

### 🍱 Table & Kitchen Management
- **Table Transfers**: Move items from one table to another or merge bills atomically.
- **KOT (Kitchen Order Ticket)**: Hold orders and automatically print instruction tickets for the kitchen.
- **Table Status**: Visual indicators for Occupied, Current, and Available tables.

### 🇮🇳 Indian GST Compliance
- **Dual Scheme Support**: Supports both **Regular GST** (Tax Invoices with breakdown) and **Composition Scheme** (Bill of Supply).
- **Auto-Title**: Automatically switches receipt headers based on legal settings.
- **Rounding**: Configurable round-off logic to the nearest rupee.

---

## 3. User Guide

### 3.1 Creating a Bill
1. Press **Ctrl+F** or click the search box.
2. Type the item name or scan a barcode.
3. Press **Enter** to add the item to the cart.
4. Adjust quantities using the **+/-** buttons or by typing directly in the QTY column.
5. Click **Save & Print** (or press **F4**) to finalize the order.

### 3.2 Moving a Table
1. In the "Current Bill" panel, click **Move Items**.
2. Select the target table from the layout popup.
3. The items will be moved, and the old table will be cleared.

### 3.3 Configuring Settings
- Go to the **Settings** tab.
- **Hotel Profile**: Set your name, address, and GSTIN.
- **Printer**: Select your default thermal printer and choose between **Thermal** or **A4** formats.
- **GST Scheme**: Toggle "GST Composition Scheme" if your business does not collect tax from customers.

---

## 4. Technical Setup

### Prerequisites
- Visual Studio 2022 (v17.10+)
- .NET 10 SDK
- (Optional) MS SQL Server for production data.

### Building the Project
1. Open `HotelPOS.slnx` in Visual Studio.
2. Ensure the solution configuration is set to `Debug` or `Release`.
3. Right-click the **HotelPOS** project and select **Set as Startup Project**.
4. Press **F5** to run.

### Database Configuration
- By default, the app uses **SQLite** for development (saved in the bin folder).
- To switch to SQL Server, update the connection string in `appsettings.json`.

---

## 5. Keyboard Shortcuts
| Key | Action |
| :--- | :--- |
| **F1 / Ctrl+F** | Focus Search Box |
| **F4** | Save & Print Bill (Checkout) |
| **Up / Down** | Navigate Auto-complete list |
| **Enter** | Select Item / Add to Cart |
| **Escape** | Close Popups / Clear Search |

---

## 6. Project Structure
For detailed architectural info, refer to [SystemDesign.md](file:///d:/HotelPOS/SystemDesign.md).

- **HotelPOS**: The main UI (WPF/XAML).
- **HotelPOS.Domain**: Core data models and entities.
- **HotelPOS.Application**: Business logic and service interfaces.
- **HotelPOS.Persistence**: Entity Framework database context and migrations.
- **HotelPOS.Infrastructure**: Theme, Printing, and Notification implementations.
- **HotelPOS.Tests**: Comprehensive unit test suite (XUnit/Moq).
