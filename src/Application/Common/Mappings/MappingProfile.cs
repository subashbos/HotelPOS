using AutoMapper;
using HotelPOS.Application.DTOs.Attendance;
using HotelPOS.Application.DTOs.Category;
using HotelPOS.Application.DTOs.Employee;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.DTOs.Leave;
using HotelPOS.Application.DTOs.Payroll;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ── Item ──────────────────────────────────────────────────────────
            CreateMap<CreateItemDto, CreateItemCommand>();
            CreateMap<CreateItemDto, Item>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));
            CreateMap<CreateItemDto, UpdateItemCommand>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()); // Id comes from route
            CreateMap<Item, ItemDto>().ReverseMap();

            // ── Table ─────────────────────────────────────────────────────────
            CreateMap<CreateTableDto, Table>();
            CreateMap<Table, TableDto>().ReverseMap();

            // ── Category ─────────────────────────────────────────────────────
            CreateMap<SaveCategoryDto, Category>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));
            CreateMap<Category, CategoryDto>().ReverseMap();

            // ── Supplier ──────────────────────────────────────────────────────
            CreateMap<SaveSupplierDto, Supplier>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Gstin, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Gstin) ? null : src.Gstin.Trim().ToUpperInvariant()));
            CreateMap<Supplier, SaveSupplierDto>();
            CreateMap<Supplier, SupplierDto>().ReverseMap();

            // ── Expense ───────────────────────────────────────────────────────
            CreateMap<Expense, SaveExpenseDto>();

            // ── User ──────────────────────────────────────────────────────────
            CreateMap<AddUserCommand, User>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username.Trim()))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
            CreateMap<User, HotelPOS.Application.DTOs.User.UserDto>().ReverseMap();
            CreateMap<Role, HotelPOS.Application.DTOs.User.RoleDto>().ReverseMap();

            // ── Order ─────────────────────────────────────────────────────────
            CreateMap<Order, HotelPOS.Application.DTOs.Order.OrderDto>().ReverseMap();
            CreateMap<OrderItem, HotelPOS.Application.DTOs.Order.OrderItemDto>().ReverseMap();

            // ── Audit ─────────────────────────────────────────────────────────
            CreateMap<AuditLog, HotelPOS.Application.DTOs.Audit.AuditLogDto>().ReverseMap();

            // ── Human Resources: Employee / Department / Designation ────────────
            CreateMap<Department, DepartmentDto>();
            CreateMap<Designation, DesignationDto>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null));

            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.DesignationTitle, opt => opt.MapFrom(src => src.Designation != null ? src.Designation.Title : null));

            CreateMap<SaveEmployeeDto, Employee>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
                .ForMember(dest => dest.EmployeeCode, opt => opt.MapFrom(src => (src.EmployeeCode ?? string.Empty).Trim()));

            // ── Human Resources: Attendance ──────────────────────────────────
            CreateMap<Attendance, AttendanceDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src =>
                    src.Employee != null ? (src.Employee.FirstName + " " + src.Employee.LastName).Trim() : null));
            CreateMap<MarkAttendanceDto, Attendance>();

            // ── Human Resources: Leave ────────────────────────────────────────
            CreateMap<LeaveType, LeaveTypeDto>();
            CreateMap<LeaveBalance, LeaveBalanceDto>()
                .ForMember(dest => dest.LeaveTypeName, opt => opt.MapFrom(src => src.LeaveType != null ? src.LeaveType.Name : null));
            CreateMap<LeaveRequest, LeaveRequestDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src =>
                    src.Employee != null ? (src.Employee.FirstName + " " + src.Employee.LastName).Trim() : null))
                .ForMember(dest => dest.LeaveTypeName, opt => opt.MapFrom(src => src.LeaveType != null ? src.LeaveType.Name : null));
            CreateMap<ApplyLeaveDto, LeaveRequest>();

            // ── Human Resources: Payroll ──────────────────────────────────────
            CreateMap<SalaryStructure, SalaryStructureDto>();
            CreateMap<SaveSalaryStructureDto, SalaryStructure>();
            CreateMap<Payslip, PayslipDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src =>
                    src.Employee != null ? (src.Employee.FirstName + " " + src.Employee.LastName).Trim() : null));
            CreateMap<PayrollRun, PayrollRunDto>();
        }
    }
}
