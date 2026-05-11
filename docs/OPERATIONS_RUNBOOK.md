# HotelPOS Operations Runbook

This document provides essential information for system administrators and managers for maintaining the HotelPOS system.

## 1. Initial Setup & Credentials

- **Default Admin Account**:
  - Username: `admin`
  - Password: `admin` (You will be prompted to change this on first login).
- **Database**:
  - By default, the system uses **SQLite** (`HotelPOS.db`) for local standalone installations.
  - For multi-terminal setups, configure **MS SQL Server** in the connection string.

## 2. Backup & Restore

### Automated Backups
- The system automatically creates a database backup every time the application is closed or on a schedule (if configured).
- **Default Location**: `[Installation Directory]/Backups/`
- **Retention**: The system keeps the last 7 days of backups and automatically cleans up older files.

### Manual Restore
1. Close all running instances of HotelPOS.
2. Locate the most recent `.db` (SQLite) or `.bak` (SQL Server) file.
3. Replace the current `HotelPOS.db` with the backup file (rename it back to `HotelPOS.db`).

## 3. Printer Configuration

- **Supported Printers**: Any Windows-compatible Thermal (80mm) or A4 printer.
- **Silent Printing**:
  - Set the "Default Printer" in the **Settings > Hardware** tab.
  - Disable "Show Print Preview" to enable 1-click checkout.
- **Troubleshooting**:
  - If printing fails, the system will fallback to showing the manual "Select Printer" dialog.
  - Ensure the printer name matches exactly as shown in Windows "Printers & Scanners".

## 4. Financial & Compliance Rules

### Fiscal Year
- The system follows the Indian Fiscal Year (April 1 to March 31).
- Invoice numbers reset automatically on April 1 (e.g., `INV/2026-27/0001`).

### GST Schemes
- **Regular**: Full tax invoice with CGST/SGST/IGST breakdown.
- **Composition**: "Bill of Supply" title, no tax collection from customers.

### Round-Off
- Can be enabled in Settings to round the final bill to the nearest Rupee.

## 5. Daily Procedures

### Cash Sessions
- **Opening**: Managers must open a cash session at the start of the day with an "Opening Balance".
- **Closing**: At the end of the shift, perform a "Cash Close" to reconcile actual cash vs. system sales.

### Troubleshooting Logs
- If the application crashes, check the **Audit Logs** tab in Settings or view the `Logs/` directory for detailed exception traces.
