using HotelPOS.Application.DTOs.Attendance;
using HotelPOS.Application.DTOs.CashSession;
using HotelPOS.Application.DTOs.Category;
using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Application.DTOs.Employee;
using HotelPOS.Application.DTOs.Estimation;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.DTOs.Leave;
using HotelPOS.Application.DTOs.Payroll;
using HotelPOS.Application.DTOs.Purchase;
using HotelPOS.Application.DTOs.Reservation;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.DTOs.UnitOfMeasurement;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Domain.Entities;
using Mapster;
using MapsterMapper;

namespace HotelPOS.Application.Common.Mappings
{
    /// <summary>
    /// Central Mapster configuration, mirroring what used to be an AutoMapper Profile.
    /// Each call site builds its own isolated TypeAdapterConfig via CreateMapper()/CreateConfig()
    /// rather than mutating the process-wide TypeAdapterConfig.GlobalSettings, matching the
    /// previously-isolated per-call-site MapperConfiguration instances.
    /// </summary>
    public static class MappingProfile
    {
        public static TypeAdapterConfig CreateConfig()
        {
            var config = new TypeAdapterConfig();
            Configure(config);
            return config;
        }

        public static IMapper CreateMapper() => new Mapper(CreateConfig());

        public static void Configure(TypeAdapterConfig config)
        {
            CreateCatalogMaps(config);
            CreateUserOrderAndAuditMaps(config);
            CreateEmployeeAndAttendanceMaps(config);
            CreateLeaveAndPayrollMaps(config);
            CreatePurchaseMaps(config);
            CreateEstimationMaps(config);
            CreateReservationMaps(config);
        }

        private static void CreateCatalogMaps(TypeAdapterConfig config)
        {
            // ── Item ──────────────────────────────────────────────────────────
            config.NewConfig<CreateItemDto, CreateItemCommand>();
            config.NewConfig<CreateItemDto, Item>()
                .Map(dest => dest.Name, src => src.Name.Trim());
            config.NewConfig<CreateItemDto, UpdateItemCommand>()
                .Ignore(dest => dest.Id); // Id comes from route
            config.NewConfig<Item, ItemDto>().TwoWays();

            // ── Table ─────────────────────────────────────────────────────────
            config.NewConfig<CreateTableDto, Table>();
            config.NewConfig<Table, TableDto>().TwoWays();
            config.NewConfig<CreateTableDto, TableDto>();

            // ── Category ─────────────────────────────────────────────────────
            config.NewConfig<SaveCategoryDto, Category>()
                .Map(dest => dest.Name, src => src.Name.Trim());
            config.NewConfig<Category, CategoryDto>().TwoWays();

            // ── Unit of Measurement ─────────────────────────────────────────────
            config.NewConfig<SaveUnitOfMeasurementDto, HotelPOS.Domain.Entities.UnitOfMeasurement>()
                .Map(dest => dest.Name, src => src.Name.Trim());
            config.NewConfig<HotelPOS.Domain.Entities.UnitOfMeasurement, UnitOfMeasurementDto>().TwoWays();

            // ── Supplier ──────────────────────────────────────────────────────
            config.NewConfig<SaveSupplierDto, Supplier>()
                .Map(dest => dest.Name, src => src.Name.Trim())
                .Map(dest => dest.Gstin, src =>
                    string.IsNullOrWhiteSpace(src.Gstin) ? null : src.Gstin.Trim().ToUpperInvariant());
            config.NewConfig<Supplier, SaveSupplierDto>();
            config.NewConfig<Supplier, SupplierDto>().TwoWays();

            // ── Expense ───────────────────────────────────────────────────────
            config.NewConfig<Expense, SaveExpenseDto>();
            config.NewConfig<SaveExpenseDto, Expense>()
                .Map(dest => dest.Title, src => src.Title.Trim())
                .Ignore(dest => dest.User);
            config.NewConfig<Expense, ExpenseDto>()
                .Map(dest => dest.CreatedByUsername, src => src.User != null ? src.User.Username : null);

            // ── Cash Session (Shift) ─────────────────────────────────────────
            config.NewConfig<CashSession, CashSessionDto>();

            // ── Customer ──────────────────────────────────────────────────────
            config.NewConfig<SaveCustomerDto, Customer>()
                .Map(dest => dest.Name, src => src.Name.Trim())
                .Map(dest => dest.Gstin, src =>
                    string.IsNullOrWhiteSpace(src.Gstin) ? null : src.Gstin.Trim().ToUpperInvariant());
            config.NewConfig<Customer, CustomerDto>().TwoWays();
        }

        private static void CreateUserOrderAndAuditMaps(TypeAdapterConfig config)
        {
            // ── User ──────────────────────────────────────────────────────────
            config.NewConfig<AddUserCommand, User>()
                .Map(dest => dest.Username, src => src.Username.Trim())
                .Ignore(dest => dest.PasswordHash)
                .Ignore(dest => dest.Salt)
                .Map(dest => dest.IsActive, src => true);
            config.NewConfig<User, HotelPOS.Application.DTOs.User.UserDto>().TwoWays();
            config.NewConfig<Role, HotelPOS.Application.DTOs.User.RoleDto>().TwoWays();

            // ── Order ─────────────────────────────────────────────────────────
            config.NewConfig<Order, HotelPOS.Application.DTOs.Order.OrderDto>().TwoWays();
            config.NewConfig<OrderItem, HotelPOS.Application.DTOs.Order.OrderItemDto>().TwoWays();

            // ── Audit ─────────────────────────────────────────────────────────
            config.NewConfig<AuditLog, HotelPOS.Application.DTOs.Audit.AuditLogDto>().TwoWays();
        }

        private static void CreateEmployeeAndAttendanceMaps(TypeAdapterConfig config)
        {
            // ── Human Resources: Employee / Department / Designation ────────────
            config.NewConfig<Department, DepartmentDto>();
            config.NewConfig<Designation, DesignationDto>()
                .Map(dest => dest.DepartmentName, src => src.Department != null ? src.Department.Name : null);

            config.NewConfig<Employee, EmployeeDto>()
                .Map(dest => dest.DepartmentName, src => src.Department != null ? src.Department.Name : null)
                .Map(dest => dest.DesignationTitle, src => src.Designation != null ? src.Designation.Title : null);

            config.NewConfig<SaveEmployeeDto, Employee>()
                .Map(dest => dest.FirstName, src => src.FirstName.Trim())
                .Map(dest => dest.EmployeeCode, src => (src.EmployeeCode ?? string.Empty).Trim());

            // ── Human Resources: Attendance ──────────────────────────────────
            config.NewConfig<Attendance, AttendanceDto>()
                .Map(dest => dest.EmployeeName, src =>
                    src.Employee != null ? (src.Employee.FirstName + " " + src.Employee.LastName).Trim() : null);
            config.NewConfig<MarkAttendanceDto, Attendance>();
        }

        private static void CreateLeaveAndPayrollMaps(TypeAdapterConfig config)
        {
            // ── Human Resources: Leave ────────────────────────────────────────
            config.NewConfig<LeaveType, LeaveTypeDto>();
            config.NewConfig<LeaveBalance, LeaveBalanceDto>()
                .Map(dest => dest.LeaveTypeName, src => src.LeaveType != null ? src.LeaveType.Name : null);
            config.NewConfig<LeaveRequest, LeaveRequestDto>()
                .Map(dest => dest.EmployeeName, src =>
                    src.Employee != null ? (src.Employee.FirstName + " " + src.Employee.LastName).Trim() : null)
                .Map(dest => dest.LeaveTypeName, src => src.LeaveType != null ? src.LeaveType.Name : null);
            config.NewConfig<ApplyLeaveDto, LeaveRequest>();

            // ── Human Resources: Payroll ──────────────────────────────────────
            config.NewConfig<SalaryStructure, SalaryStructureDto>();
            config.NewConfig<SaveSalaryStructureDto, SalaryStructure>();
            config.NewConfig<Payslip, PayslipDto>()
                .Map(dest => dest.EmployeeName, src =>
                    src.Employee != null ? (src.Employee.FirstName + " " + src.Employee.LastName).Trim() : null);
            config.NewConfig<PayrollRun, PayrollRunDto>();
        }

        private static void CreatePurchaseMaps(TypeAdapterConfig config)
        {
            // ── Purchases ─────────────────────────────────────────────────────
            config.NewConfig<Purchase, PurchaseDto>()
                .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : null)
                .Map(dest => dest.Items, src => src.PurchaseItems.Adapt<List<PurchaseItemDto>>());
            config.NewConfig<PurchaseItem, PurchaseItemDto>();
        }

        private static void CreateEstimationMaps(TypeAdapterConfig config)
        {
            // ── Estimations ───────────────────────────────────────────────────
            config.NewConfig<Estimation, EstimationDto>()
                .Map(dest => dest.Items, src => src.EstimationItems.Adapt<List<EstimationItemDto>>());
            config.NewConfig<EstimationItem, EstimationItemDto>();
        }

        private static void CreateReservationMaps(TypeAdapterConfig config)
        {
            // ── Reservations ──────────────────────────────────────────────────
            config.NewConfig<Reservation, ReservationDto>()
                .Map(dest => dest.TableName, src => src.Table != null ? src.Table.Name : null);
        }
    }
}
