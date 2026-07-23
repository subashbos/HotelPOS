using AutoMapper;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.Common.Mappings;
using HotelPOS.Application.DTOs.Audit;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.DTOs.Purchase;
using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.DTOs.Setting;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Controllers
{
    /// <summary>
    /// Controller-level tests for previously-uncovered API controllers: Audit, Suppliers,
    /// Purchases, Tables, Roles, Settings, Expenses and Reports.
    /// </summary>
    public class ApiControllersExtendedTests
    {
        private static readonly IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        // ================= AuditController =================

        [Fact]
        public async Task Audit_GetLogs_ReturnsOkWithLogs()
        {
            var svc = new Mock<IAuditService>();
            svc.Setup(s => s.GetLogsAsync(null, null)).ReturnsAsync(new List<AuditLogDto>
            {
                new AuditLogDto { Id = 1, EntityName = "Item", EntityId = 5, Action = "Update" }
            });

            var controller = new AuditController(svc.Object);
            var result = await controller.GetLogs(null, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var logs = Assert.IsAssignableFrom<IEnumerable<AuditLogDto>>(ok.Value);
            Assert.Single(logs);
        }

        [Fact]
        public async Task Audit_GetLogs_PassesDateRangeThrough()
        {
            var from = new DateTime(2026, 1, 1);
            var to = new DateTime(2026, 1, 31);
            var svc = new Mock<IAuditService>();
            svc.Setup(s => s.GetLogsAsync(from, to)).ReturnsAsync(new List<AuditLogDto>());

            var controller = new AuditController(svc.Object);
            await controller.GetLogs(from, to);

            svc.Verify(s => s.GetLogsAsync(from, to), Times.Once);
        }

        // ================= SuppliersController =================

        [Fact]
        public async Task Suppliers_GetSuppliers_ReturnsMappedDtos()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.GetSuppliersAsync()).ReturnsAsync(new List<Supplier>
            {
                new Supplier { Id = 1, Name = "Acme Foods" }
            });

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.GetSuppliers();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<SupplierDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Suppliers_GetSupplier_InvalidId_ReturnsBadRequest()
        {
            var controller = new SuppliersController(Mock.Of<ISupplierService>(), Mapper);
            var result = await controller.GetSupplier(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Suppliers_GetSupplier_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.GetSupplierByIdAsync(99)).ReturnsAsync((Supplier?)null);

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.GetSupplier(99);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Suppliers_GetSupplier_Found_ReturnsDto()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.GetSupplierByIdAsync(1)).ReturnsAsync(new Supplier { Id = 1, Name = "Acme" });

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.GetSupplier(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<SupplierDto>(ok.Value);
            Assert.Equal("Acme", dto.Name);
        }

        [Fact]
        public async Task Suppliers_CreateSupplier_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new SuppliersController(Mock.Of<ISupplierService>(), Mapper);
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.CreateSupplier(new SaveSupplierDto { Name = "" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Suppliers_CreateSupplier_Valid_ReturnsCreatedAtAction()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.SaveSupplierAsync(It.IsAny<Supplier>()))
                .Callback<Supplier>(s => s.Id = 7)
                .Returns(Task.CompletedTask);

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.CreateSupplier(new SaveSupplierDto { Name = "New Supplier" });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<SupplierDto>(created.Value);
            Assert.Equal(7, dto.Id);
        }

        [Fact]
        public async Task Suppliers_CreateSupplier_ArgumentException_ReturnsBadRequest()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.SaveSupplierAsync(It.IsAny<Supplier>())).ThrowsAsync(new ArgumentException("bad"));

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.CreateSupplier(new SaveSupplierDto { Name = "Dup" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Suppliers_CreateSupplier_InvalidOperationException_ReturnsConflict()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.SaveSupplierAsync(It.IsAny<Supplier>())).ThrowsAsync(new InvalidOperationException("dup"));

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.CreateSupplier(new SaveSupplierDto { Name = "Dup" });

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Suppliers_UpdateSupplier_InvalidId_ReturnsBadRequest()
        {
            var controller = new SuppliersController(Mock.Of<ISupplierService>(), Mapper);
            var result = await controller.UpdateSupplier(0, new SaveSupplierDto { Name = "X" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Suppliers_UpdateSupplier_Valid_ReturnsNoContent()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.SaveSupplierAsync(It.IsAny<Supplier>())).Returns(Task.CompletedTask);

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.UpdateSupplier(3, new SaveSupplierDto { Name = "Updated" });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Suppliers_UpdateSupplier_InvalidOperationException_ReturnsConflict()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.SaveSupplierAsync(It.IsAny<Supplier>())).ThrowsAsync(new InvalidOperationException("dup"));

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.UpdateSupplier(3, new SaveSupplierDto { Name = "Dup" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Suppliers_DeleteSupplier_InvalidId_ReturnsBadRequest()
        {
            var controller = new SuppliersController(Mock.Of<ISupplierService>(), Mapper);
            var result = await controller.DeleteSupplier(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Suppliers_DeleteSupplier_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.DeleteSupplierAsync(99)).ThrowsAsync(new KeyNotFoundException());

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.DeleteSupplier(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Suppliers_DeleteSupplier_InvalidOperationException_ReturnsConflict()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.DeleteSupplierAsync(1)).ThrowsAsync(new InvalidOperationException("in use"));

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.DeleteSupplier(1);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Suppliers_DeleteSupplier_Valid_ReturnsNoContent()
        {
            var svc = new Mock<ISupplierService>();
            svc.Setup(s => s.DeleteSupplierAsync(1)).Returns(Task.CompletedTask);

            var controller = new SuppliersController(svc.Object, Mapper);
            var result = await controller.DeleteSupplier(1);

            Assert.IsType<NoContentResult>(result);
        }

        // ================= PurchasesController =================

        [Fact]
        public async Task Purchases_GetPurchases_ReturnsMappedDtos()
        {
            var svc = new Mock<IPurchaseService>();
            svc.Setup(s => s.GetPurchasesAsync()).ReturnsAsync(new List<Purchase>
            {
                new Purchase { Id = 1, InvoiceNumber = "INV-1" }
            });

            var controller = new PurchasesController(svc.Object, Mapper);
            var result = await controller.GetPurchases();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<PurchaseDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Purchases_GetSuppliers_ReturnsMappedDtos()
        {
            var svc = new Mock<IPurchaseService>();
            svc.Setup(s => s.GetSuppliersAsync()).ReturnsAsync(new List<Supplier>
            {
                new Supplier { Id = 1, Name = "Acme" }
            });

            var controller = new PurchasesController(svc.Object, Mapper);
            var result = await controller.GetSuppliers();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<SupplierDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Purchases_CreatePurchase_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new PurchasesController(Mock.Of<IPurchaseService>(), Mapper);
            controller.ModelState.AddModelError("InvoiceNumber", "Required");

            var result = await controller.CreatePurchase(new SavePurchaseDto());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Purchases_CreatePurchase_NoItems_ReturnsBadRequest()
        {
            var controller = new PurchasesController(Mock.Of<IPurchaseService>(), Mapper);
            var result = await controller.CreatePurchase(new SavePurchaseDto { Items = new List<SavePurchaseItemDto>() });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Purchases_CreatePurchase_ComputesTotalsServerSide()
        {
            Purchase? captured = null;
            var svc = new Mock<IPurchaseService>();
            svc.Setup(s => s.SavePurchaseAsync(It.IsAny<Purchase>()))
                .Callback<Purchase>(p => captured = p)
                .Returns(Task.CompletedTask);

            var controller = new PurchasesController(svc.Object, Mapper);
            var request = new SavePurchaseDto
            {
                SupplierId = 1,
                InvoiceNumber = "INV-100",
                PurchaseDate = DateTime.Today,
                PaymentType = "Cash",
                TotalDiscount = 10,
                Items = new List<SavePurchaseItemDto>
                {
                    new SavePurchaseItemDto { ItemId = 1, ItemName = "Rice", Quantity = 10, UnitPrice = 50, TaxPercentage = 5, Discount = 5 }
                }
            };

            var result = await controller.CreatePurchase(request);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(captured);
            // Subtotal = 10 * 50 = 500; Tax = 500 * 5% = 25; Discount = 5 (line) + 10 (overall) = 15
            Assert.Equal(500m, captured!.Subtotal);
            Assert.Equal(25m, captured.TotalTax);
            Assert.Equal(15m, captured.TotalDiscount);
            Assert.Equal(510m, captured.GrandTotal); // 500 + 25 - 15
        }

        [Fact]
        public async Task Purchases_CreatePurchase_GrandTotalNeverNegative()
        {
            var svc = new Mock<IPurchaseService>();
            svc.Setup(s => s.SavePurchaseAsync(It.IsAny<Purchase>())).Returns(Task.CompletedTask);

            var controller = new PurchasesController(svc.Object, Mapper);
            var request = new SavePurchaseDto
            {
                SupplierId = 1,
                InvoiceNumber = "INV-101",
                TotalDiscount = 10000, // wildly exceeds subtotal + tax
                Items = new List<SavePurchaseItemDto>
                {
                    new SavePurchaseItemDto { ItemId = 1, ItemName = "Rice", Quantity = 1, UnitPrice = 10, TaxPercentage = 0, Discount = 0 }
                }
            };

            var result = await controller.CreatePurchase(request);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<PurchaseDto>(created.Value);
            Assert.Equal(0m, dto.GrandTotal);
        }

        [Fact]
        public async Task Purchases_CreatePurchase_ArgumentException_ReturnsBadRequest()
        {
            var svc = new Mock<IPurchaseService>();
            svc.Setup(s => s.SavePurchaseAsync(It.IsAny<Purchase>())).ThrowsAsync(new ArgumentException("bad supplier"));

            var controller = new PurchasesController(svc.Object, Mapper);
            var request = new SavePurchaseDto
            {
                SupplierId = 999,
                InvoiceNumber = "INV-1",
                Items = new List<SavePurchaseItemDto>
                {
                    new SavePurchaseItemDto { ItemId = 1, ItemName = "Rice", Quantity = 1, UnitPrice = 10 }
                }
            };

            var result = await controller.CreatePurchase(request);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // ================= TablesController =================

        [Fact]
        public async Task Tables_GetTables_ReturnsMappedDtos()
        {
            var svc = new Mock<ITableService>();
            svc.Setup(s => s.GetTablesAsync()).ReturnsAsync(new List<Table>
            {
                new Table { Id = 1, Number = 1, Name = "T1", Capacity = 4 }
            });

            var controller = new TablesController(svc.Object, Mapper);
            var result = await controller.GetTables();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<TableDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Tables_CreateTable_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new TablesController(Mock.Of<ITableService>(), Mapper);
            controller.ModelState.AddModelError("Number", "Required");

            var result = await controller.CreateTable(new CreateTableDto());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Tables_CreateTable_Valid_ReturnsCreatedAtAction()
        {
            var svc = new Mock<ITableService>();
            svc.Setup(s => s.AddTableAsync(It.IsAny<CreateTableDto>())).ReturnsAsync(9);

            var controller = new TablesController(svc.Object, Mapper);
            var result = await controller.CreateTable(new CreateTableDto { Number = 5, Name = "T5", Capacity = 2 });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<TableDto>(created.Value);
            Assert.Equal(9, dto.Id);
        }

        [Fact]
        public async Task Tables_CreateTable_DuplicateNumber_ReturnsConflict()
        {
            var svc = new Mock<ITableService>();
            svc.Setup(s => s.AddTableAsync(It.IsAny<CreateTableDto>())).ThrowsAsync(new InvalidOperationException("dup"));

            var controller = new TablesController(svc.Object, Mapper);
            var result = await controller.CreateTable(new CreateTableDto { Number = 5, Name = "T5", Capacity = 2 });

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Tables_UpdateTable_InvalidId_ReturnsBadRequest()
        {
            var controller = new TablesController(Mock.Of<ITableService>(), Mapper);
            var result = await controller.UpdateTable(0, new CreateTableDto { Number = 1, Name = "T1", Capacity = 2 });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Tables_UpdateTable_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ITableService>();
            svc.Setup(s => s.UpdateTableAsync(99, It.IsAny<CreateTableDto>())).ThrowsAsync(new KeyNotFoundException());

            var controller = new TablesController(svc.Object, Mapper);
            var result = await controller.UpdateTable(99, new CreateTableDto { Number = 1, Name = "T1", Capacity = 2 });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Tables_UpdateTable_Conflict_ReturnsConflict()
        {
            var svc = new Mock<ITableService>();
            svc.Setup(s => s.UpdateTableAsync(1, It.IsAny<CreateTableDto>())).ThrowsAsync(new InvalidOperationException("dup"));

            var controller = new TablesController(svc.Object, Mapper);
            var result = await controller.UpdateTable(1, new CreateTableDto { Number = 1, Name = "T1", Capacity = 2 });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Tables_UpdateTable_Valid_ReturnsNoContent()
        {
            var svc = new Mock<ITableService>();
            svc.Setup(s => s.UpdateTableAsync(1, It.IsAny<CreateTableDto>())).Returns(Task.CompletedTask);

            var controller = new TablesController(svc.Object, Mapper);
            var result = await controller.UpdateTable(1, new CreateTableDto { Number = 1, Name = "T1", Capacity = 2 });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Tables_DeleteTable_InvalidId_ReturnsBadRequest()
        {
            var controller = new TablesController(Mock.Of<ITableService>(), Mapper);
            var result = await controller.DeleteTable(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Tables_DeleteTable_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ITableService>();
            svc.Setup(s => s.DeleteTableAsync(99)).ThrowsAsync(new KeyNotFoundException());

            var controller = new TablesController(svc.Object, Mapper);
            var result = await controller.DeleteTable(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Tables_DeleteTable_Valid_ReturnsNoContent()
        {
            var svc = new Mock<ITableService>();
            svc.Setup(s => s.DeleteTableAsync(1)).Returns(Task.CompletedTask);

            var controller = new TablesController(svc.Object, Mapper);
            var result = await controller.DeleteTable(1);

            Assert.IsType<NoContentResult>(result);
        }

        // ================= RolesController =================

        [Fact]
        public async Task Roles_GetRoles_ReturnsOkWithRoles()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.GetAllRolesAsync()).ReturnsAsync(new List<Role>
            {
                new Role { Id = 1, Name = "Admin" }
            });

            var controller = new RolesController(svc.Object);
            var result = await controller.GetRoles();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<RoleDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Roles_GetRole_InvalidId_ReturnsBadRequest()
        {
            var controller = new RolesController(Mock.Of<IRoleService>());
            var result = await controller.GetRole(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Roles_GetRole_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.GetRoleByIdAsync(99)).ReturnsAsync((Role?)null);

            var controller = new RolesController(svc.Object);
            var result = await controller.GetRole(99);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Roles_GetRole_Found_ReturnsDto()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.GetRoleByIdAsync(1)).ReturnsAsync(new Role { Id = 1, Name = "Manager" });

            var controller = new RolesController(svc.Object);
            var result = await controller.GetRole(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<RoleDto>(ok.Value);
            Assert.Equal("Manager", dto.Name);
        }

        [Fact]
        public async Task Roles_CreateRole_EmptyName_ReturnsBadRequest()
        {
            var controller = new RolesController(Mock.Of<IRoleService>());
            var result = await controller.CreateRole(new CreateRoleRequest { Name = "" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Roles_CreateRole_DuplicateName_ReturnsConflict()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.AddRoleAsync("Chef", "")).ReturnsAsync(false);

            var controller = new RolesController(svc.Object);
            var result = await controller.CreateRole(new CreateRoleRequest { Name = "Chef" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Roles_CreateRole_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.AddRoleAsync("Chef", "Kitchen staff")).ReturnsAsync(true);

            var controller = new RolesController(svc.Object);
            var result = await controller.CreateRole(new CreateRoleRequest { Name = "Chef", Description = "Kitchen staff" });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Roles_UpdatePermissions_InvalidId_ReturnsBadRequest()
        {
            var controller = new RolesController(Mock.Of<IRoleService>());
            var result = await controller.UpdatePermissions(0, new List<RolePermission>());
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Roles_UpdatePermissions_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.UpdateRolePermissionsAsync(99, It.IsAny<List<RolePermission>>())).ThrowsAsync(new KeyNotFoundException());

            var controller = new RolesController(svc.Object);
            var result = await controller.UpdatePermissions(99, new List<RolePermission>());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Roles_UpdatePermissions_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.UpdateRolePermissionsAsync(1, It.IsAny<List<RolePermission>>())).Returns(Task.CompletedTask);

            var controller = new RolesController(svc.Object);
            var result = await controller.UpdatePermissions(1, new List<RolePermission>());

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Roles_DeleteRole_InvalidId_ReturnsBadRequest()
        {
            var controller = new RolesController(Mock.Of<IRoleService>());
            var result = await controller.DeleteRole(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Roles_DeleteRole_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.DeleteRoleAsync(99)).ThrowsAsync(new KeyNotFoundException());

            var controller = new RolesController(svc.Object);
            var result = await controller.DeleteRole(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Roles_DeleteRole_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IRoleService>();
            svc.Setup(s => s.DeleteRoleAsync(1)).Returns(Task.CompletedTask);

            var controller = new RolesController(svc.Object);
            var result = await controller.DeleteRole(1);

            Assert.IsType<NoContentResult>(result);
        }

        // ================= SettingsController =================

        [Fact]
        public async Task Settings_GetSettings_SmtpPasswordSet_TrueWhenPasswordPresent()
        {
            var svc = new Mock<ISettingService>();
            svc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting { SmtpPassword = "secret" });

            var controller = new SettingsController(svc.Object, TestAuthorization.AllowAll().Object);
            var result = await controller.GetSettings();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<SettingsDto>(ok.Value);
            Assert.True(dto.SmtpPasswordSet);
        }

        [Fact]
        public async Task Settings_GetSettings_SmtpPasswordSet_FalseWhenPasswordEmpty()
        {
            var svc = new Mock<ISettingService>();
            svc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting { SmtpPassword = null });

            var controller = new SettingsController(svc.Object, TestAuthorization.AllowAll().Object);
            var result = await controller.GetSettings();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<SettingsDto>(ok.Value);
            Assert.False(dto.SmtpPasswordSet);
        }

        [Fact]
        public async Task Settings_GetSettings_WithoutSettingsPermission_RedactsSmtpAndBackupFields()
        {
            var svc = new Mock<ISettingService>();
            svc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting
            {
                HotelName = "Hotel X",
                SmtpHost = "smtp.example.com",
                SmtpPort = 587,
                SmtpUsername = "billing@example.com",
                SmtpPassword = "secret",
                SmtpUseSsl = true,
                SmtpFromAddress = "billing@example.com",
                OffsiteBackupPath = @"\\backup-server\hotelpos"
            });

            var controller = new SettingsController(svc.Object, TestAuthorization.DenyAll().Object);
            var result = await controller.GetSettings();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<SettingsDto>(ok.Value);

            // Operational fields any authenticated user needs for billing stay visible...
            Assert.Equal("Hotel X", dto.HotelName);
            // ...but SMTP credentials and the backup destination are redacted to their zero values.
            Assert.Null(dto.SmtpHost);
            Assert.Equal(0, dto.SmtpPort);
            Assert.Null(dto.SmtpUsername);
            Assert.False(dto.SmtpPasswordSet);
            Assert.False(dto.SmtpUseSsl);
            Assert.Null(dto.SmtpFromAddress);
            Assert.Null(dto.OffsiteBackupPath);
        }

        [Fact]
        public async Task Settings_SaveSettings_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new SettingsController(Mock.Of<ISettingService>(), TestAuthorization.AllowAll().Object);
            controller.ModelState.AddModelError("HotelName", "Required");

            var result = await controller.SaveSettings(new SaveSettingsDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Settings_SaveSettings_EmptyPassword_PreservesExistingPassword()
        {
            SystemSetting? saved = null;
            var svc = new Mock<ISettingService>();
            svc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting { Id = 1, SmtpPassword = "existing-secret" });
            svc.Setup(s => s.SaveSettingsAsync(It.IsAny<SystemSetting>()))
                .Callback<SystemSetting>(s => saved = s)
                .Returns(Task.CompletedTask);

            var controller = new SettingsController(svc.Object, TestAuthorization.AllowAll().Object);
            var result = await controller.SaveSettings(new SaveSettingsDto { HotelName = "Hotel X", SmtpPassword = null });

            Assert.IsType<NoContentResult>(result);
            Assert.NotNull(saved);
            Assert.Equal("existing-secret", saved!.SmtpPassword);
        }

        [Fact]
        public async Task Settings_SaveSettings_NewPassword_OverwritesExistingPassword()
        {
            SystemSetting? saved = null;
            var svc = new Mock<ISettingService>();
            svc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting { Id = 1, SmtpPassword = "existing-secret" });
            svc.Setup(s => s.SaveSettingsAsync(It.IsAny<SystemSetting>()))
                .Callback<SystemSetting>(s => saved = s)
                .Returns(Task.CompletedTask);

            var controller = new SettingsController(svc.Object, TestAuthorization.AllowAll().Object);
            var result = await controller.SaveSettings(new SaveSettingsDto { HotelName = "Hotel X", SmtpPassword = "new-secret" });

            Assert.IsType<NoContentResult>(result);
            Assert.NotNull(saved);
            Assert.Equal("new-secret", saved!.SmtpPassword);
        }

        // ================= ExpensesController =================

        [Fact]
        public async Task Expenses_GetExpenses_ReturnsMappedDtos()
        {
            var svc = new Mock<IExpenseService>();
            svc.Setup(s => s.GetExpensesAsync(null, null)).ReturnsAsync(new List<Expense>
            {
                new Expense { Id = 1, Title = "Electricity", Amount = 100 }
            });

            var controller = new ExpensesController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.GetExpenses(null, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<ExpenseDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Expenses_GetExpense_InvalidId_ReturnsBadRequest()
        {
            var controller = new ExpensesController(Mock.Of<IExpenseService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.GetExpense(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Expenses_GetExpense_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IExpenseService>();
            svc.Setup(s => s.GetExpenseByIdAsync(99)).ReturnsAsync((Expense?)null);

            var controller = new ExpensesController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.GetExpense(99);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Expenses_CreateExpense_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new ExpensesController(Mock.Of<IExpenseService>(), Mock.Of<IUserContext>(), Mapper);
            controller.ModelState.AddModelError("Title", "Required");

            var result = await controller.CreateExpense(new SaveExpenseDto());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Expenses_CreateExpense_Valid_SetsCreatedByFromUserContext()
        {
            Expense? saved = null;
            var svc = new Mock<IExpenseService>();
            svc.Setup(s => s.SaveExpenseAsync(It.IsAny<Expense>()))
                .Callback<Expense>(e => { e.Id = 5; saved = e; })
                .Returns(Task.CompletedTask);
            var userCtx = new Mock<IUserContext>();
            userCtx.Setup(u => u.CurrentUserId).Returns(42);

            var controller = new ExpensesController(svc.Object, userCtx.Object, Mapper);
            var result = await controller.CreateExpense(new SaveExpenseDto { Title = "Rent", Amount = 500 });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<ExpenseDto>(created.Value);
            Assert.Equal(5, dto.Id);
            Assert.NotNull(saved);
            Assert.Equal(42, saved!.CreatedBy);
        }

        [Fact]
        public async Task Expenses_CreateExpense_ArgumentException_ReturnsBadRequest()
        {
            var svc = new Mock<IExpenseService>();
            svc.Setup(s => s.SaveExpenseAsync(It.IsAny<Expense>())).ThrowsAsync(new ArgumentException("bad"));

            var controller = new ExpensesController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.CreateExpense(new SaveExpenseDto { Title = "Rent", Amount = 500 });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Expenses_UpdateExpense_InvalidId_ReturnsBadRequest()
        {
            var controller = new ExpensesController(Mock.Of<IExpenseService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.UpdateExpense(0, new SaveExpenseDto { Title = "X", Amount = 1 });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Expenses_UpdateExpense_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IExpenseService>();
            svc.Setup(s => s.GetExpenseByIdAsync(99)).ReturnsAsync((Expense?)null);

            var controller = new ExpensesController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.UpdateExpense(99, new SaveExpenseDto { Title = "X", Amount = 1 });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Expenses_UpdateExpense_Valid_PreservesOriginalCreatedBy()
        {
            Expense? saved = null;
            var svc = new Mock<IExpenseService>();
            svc.Setup(s => s.GetExpenseByIdAsync(1)).ReturnsAsync(new Expense { Id = 1, CreatedBy = 7, Title = "Old", Amount = 10 });
            svc.Setup(s => s.SaveExpenseAsync(It.IsAny<Expense>()))
                .Callback<Expense>(e => saved = e)
                .Returns(Task.CompletedTask);

            var controller = new ExpensesController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.UpdateExpense(1, new SaveExpenseDto { Title = "Updated", Amount = 20 });

            Assert.IsType<NoContentResult>(result);
            Assert.NotNull(saved);
            Assert.Equal(7, saved!.CreatedBy);
        }

        [Fact]
        public async Task Expenses_DeleteExpense_InvalidId_ReturnsBadRequest()
        {
            var controller = new ExpensesController(Mock.Of<IExpenseService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.DeleteExpense(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Expenses_DeleteExpense_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IExpenseService>();
            svc.Setup(s => s.DeleteExpenseAsync(99)).ThrowsAsync(new KeyNotFoundException());

            var controller = new ExpensesController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.DeleteExpense(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Expenses_DeleteExpense_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IExpenseService>();
            svc.Setup(s => s.DeleteExpenseAsync(1)).Returns(Task.CompletedTask);

            var controller = new ExpensesController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.DeleteExpense(1);

            Assert.IsType<NoContentResult>(result);
        }

        // ================= ReportsController =================

        private static ReportsController CreateReportsController(
            Mock<IReportService>? reportSvc = null, Mock<IBIReportService>? biSvc = null)
        {
            return new ReportsController(
                (reportSvc ?? new Mock<IReportService>()).Object,
                (biSvc ?? new Mock<IBIReportService>()).Object);
        }

        [Fact]
        public async Task Reports_GetSalesReport_ReturnsOkWithData()
        {
            var reportSvc = new Mock<IReportService>();
            var dto = new SalesReportDto { TotalRevenue = 1000, TotalOrders = 5 };
            reportSvc.Setup(s => s.GetSalesReportAsync(null, null)).ReturnsAsync(dto);

            var controller = CreateReportsController(reportSvc);
            var result = await controller.GetSalesReport(null, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, ok.Value);
        }

        [Fact]
        public async Task Reports_GetItemReport_ReturnsOkWithData()
        {
            var reportSvc = new Mock<IReportService>();
            var rows = new List<ItemReportRowDto> { new ItemReportRowDto { ItemName = "Coffee" } };
            reportSvc.Setup(s => s.GetItemReportAsync(null, null)).ReturnsAsync(rows);

            var controller = CreateReportsController(reportSvc);
            var result = await controller.GetItemReport(null, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(rows, ok.Value);
        }

        [Fact]
        public async Task Reports_GetGstReport_ReturnsOkWithData()
        {
            var from = new DateTime(2026, 1, 1);
            var to = new DateTime(2026, 1, 31);
            var reportSvc = new Mock<IReportService>();
            var rows = new List<GstReportRowDto> { new GstReportRowDto { OrderCount = 3 } };
            reportSvc.Setup(s => s.GetGstReportAsync(from, to)).ReturnsAsync(rows);

            var controller = CreateReportsController(reportSvc);
            var result = await controller.GetGstReport(from, to);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(rows, ok.Value);
        }

        [Fact]
        public async Task Reports_GetMonthlyChart_ReturnsOkWithData()
        {
            var reportSvc = new Mock<IReportService>();
            var rows = new List<MonthlySalesChartDto> { new MonthlySalesChartDto { MonthName = "Jan", Revenue = 100 } };
            reportSvc.Setup(s => s.GetMonthlyChartDataAsync()).ReturnsAsync(rows);

            var controller = CreateReportsController(reportSvc);
            var result = await controller.GetMonthlyChart();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(rows, ok.Value);
        }

        [Fact]
        public async Task Reports_GetPurchaseReport_MapsQueryRequestAndReturnsTotals()
        {
            var reportSvc = new Mock<IReportService>();
            reportSvc.Setup(s => s.GetPagedPurchaseReportAsync(It.IsAny<PagedPurchaseReportRequest>()))
                .ReturnsAsync((new List<PurchaseReportRowDto>(), 0, 100m, 10m, 5m, 3));

            var controller = CreateReportsController(reportSvc);
            var result = await controller.GetPurchaseReport(new PurchaseReportQueryRequest { Page = 2, PageSize = 10 });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PagedPurchaseReportResponse>(ok.Value);
            Assert.Equal(100m, response.TotalPurchases);
            Assert.Equal(10m, response.TotalTax);
            Assert.Equal(5m, response.TotalDiscount);
            Assert.Equal(3, response.TotalQty);
            reportSvc.Verify(s => s.GetPagedPurchaseReportAsync(It.Is<PagedPurchaseReportRequest>(r => r.Page == 2 && r.PageSize == 10)), Times.Once);
        }

        [Fact]
        public async Task Reports_GetMarginSummary_ReturnsOkWithData()
        {
            var biSvc = new Mock<IBIReportService>();
            var dto = new ProfitMarginSummaryDto(1000, 400, 600, 100, 500, 60, 40);
            biSvc.Setup(s => s.GetProfitMarginSummaryAsync(null, null)).ReturnsAsync(dto);

            var controller = CreateReportsController(biSvc: biSvc);
            var result = await controller.GetMarginSummary(null, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, ok.Value);
        }

        [Fact]
        public async Task Reports_GetItemMargins_ReturnsOkWithData()
        {
            var biSvc = new Mock<IBIReportService>();
            var rows = new List<ItemMarginRowDto> { new ItemMarginRowDto(1, "Coffee", "Beverages", 10, 50, 20, 500, 200, 300, 60, "Keep") };
            biSvc.Setup(s => s.GetItemMarginsAsync(null, null)).ReturnsAsync(rows);

            var controller = CreateReportsController(biSvc: biSvc);
            var result = await controller.GetItemMargins(null, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(rows, ok.Value);
        }

        [Fact]
        public async Task Reports_GetWastageSummary_ReturnsOkWithData()
        {
            var biSvc = new Mock<IBIReportService>();
            var dto = new WastageSummaryDto(100, 5, new List<WastageReasonRowDto>(), new List<WastageItemRowDto>());
            biSvc.Setup(s => s.GetWastageSummaryAsync(null, null)).ReturnsAsync(dto);

            var controller = CreateReportsController(biSvc: biSvc);
            var result = await controller.GetWastageSummary(null, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, ok.Value);
        }

        [Fact]
        public async Task Reports_LogWastage_EmptyReason_ReturnsBadRequest()
        {
            var controller = CreateReportsController();
            var result = await controller.LogWastage(new LogWastageRequest { ItemId = 1, Quantity = 1, Reason = "" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Reports_LogWastage_ZeroQuantity_ReturnsBadRequest()
        {
            var controller = CreateReportsController();
            var result = await controller.LogWastage(new LogWastageRequest { ItemId = 1, Quantity = 0, Reason = "Spoiled" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Reports_LogWastage_Valid_ReturnsNoContent()
        {
            var biSvc = new Mock<IBIReportService>();
            biSvc.Setup(s => s.LogWastageAsync(1, 3, "Spoiled", null)).Returns(Task.CompletedTask);

            var controller = CreateReportsController(biSvc: biSvc);
            var result = await controller.LogWastage(new LogWastageRequest { ItemId = 1, Quantity = 3, Reason = "Spoiled" });

            Assert.IsType<NoContentResult>(result);
            biSvc.Verify(s => s.LogWastageAsync(1, 3, "Spoiled", null), Times.Once);
        }

        [Fact]
        public async Task Reports_GetLowStockAlerts_ReturnsOkWithData()
        {
            var biSvc = new Mock<IBIReportService>();
            var rows = new List<LowStockAlertDto> { new LowStockAlertDto(1, 1, "Rice", 2, 10, 1.5, 1, "Critical") };
            biSvc.Setup(s => s.GetLowStockAlertsAsync()).ReturnsAsync(rows);

            var controller = CreateReportsController(biSvc: biSvc);
            var result = await controller.GetLowStockAlerts();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(rows, ok.Value);
        }

        [Fact]
        public async Task Reports_GetMonthlyTrend_ReturnsOkWithData()
        {
            var biSvc = new Mock<IBIReportService>();
            var rows = new List<MonthlyTrendDto> { new MonthlyTrendDto("Jan", 1000, 400, 300) };
            biSvc.Setup(s => s.GetMonthlyTrendDataAsync()).ReturnsAsync(rows);

            var controller = CreateReportsController(biSvc: biSvc);
            var result = await controller.GetMonthlyTrend();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(rows, ok.Value);
        }
    }
}
