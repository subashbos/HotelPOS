# 🏨 HotelPOS - Advanced Billing & Management System

Welcome to the **HotelPOS** project. This is a professional-grade Point of Sale system built with .NET 10 and WPF, specifically optimized for Indian hospitality businesses.

---

## 📚 Project Documentation

To help you get started, we have provided comprehensive documentation split into three main areas:

### 1. [User Guide & Features (ProjectDocumentation.md)](file:///d:/HotelPOS/ProjectDocumentation.md)
*   **For**: Business owners, cashiers, and end-users.
*   **Contents**: How to create bills, manage tables, configure GST schemes, and use keyboard shortcuts.

### 2. [System Design & Architecture (SystemDesign.md)](file:///d:/HotelPOS/SystemDesign.md)
*   **For**: System architects and technical stakeholders.
*   **Contents**: Layered architecture (Clean Architecture), core modules (Billing, Printing), and technology stack.

### 3. [Technical Reference & Implementation (TechnicalReference.md)](file:///d:/HotelPOS/TechnicalReference.md)
*   **For**: Developers and maintainers.
*   **Contents**: Deep-dive into `CartService` thread safety, `BillingViewModel` logic, `FlowDocument` generation, and Database schema.

---

## 🚀 Quick Start
1. Open `HotelPOS.slnx` in Visual Studio 2022.
2. Build the solution in `Release` or `Debug` mode.
3. Run the **HotelPOS** project.
4. **Login**: Use standard credentials (or check the Users tab in Settings).
5. **Billing**: Press `F1` to search for items and `F4` to checkout.

---

## 🛠 Tech Stack
- **UI**: WPF (XAML)
- **Logic**: .NET 10, MediatR, MVVM
- **Database**: Entity Framework Core (SQL Server / SQLite)
- **Compliance**: Indian GST Ready (Tax Invoice / Bill of Supply)
