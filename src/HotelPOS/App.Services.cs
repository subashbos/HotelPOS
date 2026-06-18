using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Orders.Commands;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Application.UseCases.Roles.Commands;
using HotelPOS.Application.UseCases.Settings.Commands;
using HotelPOS.Application.UseCases.Suppliers.Commands;
using HotelPOS.Application.UseCases.CashSessions.Commands;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Application.UseCases.Auth.Commands;

using HotelPOS.Application.Interfaces;
using HotelPOS.Services;
using HotelPOS.Infrastructure;
using HotelPOS.Infrastructure.Persistence;
using HotelPOS.ViewModels;
using HotelPOS.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;
using FluentValidation;
using AutoMapper;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Common.Validators;
using HotelPOS.Application.DTOs.Table;
using System;

namespace HotelPOS
{
    public partial class App
    {
        private void ConfigureServices(IServiceCollection services, IConfiguration config, string connectionString)
        {
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            // ── Database ──────────────────────────────────────────────────────
            // Single Scoped registration — one DbContext per DI scope (= one session)
            services.AddDbContext<HotelDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
                // Suppress pending model changes warning during runtime migration
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            // ── Services ──────────────────────────────────────────────────────
            services.AddSingleton<IUserContext, UserContext>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();

            // ── Repositories (Scoped) ─────────────────────────────────────────
            services.AddInfrastructure();

            // ── Services (Scoped) ─────────────────────────────────────────────
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<IValidator<CreateItemCommand>, CreateItemCommandValidator>();
            services.AddScoped<IValidator<UpdateItemCommand>, UpdateItemCommandValidator>();
            services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();
            services.AddScoped<IValidator<Category>, CategoryValidator>();
            services.AddScoped<IValidator<CashSession>, CashSessionValidator>();
            services.AddScoped<IValidator<Purchase>, PurchaseValidator>();
            services.AddScoped<IValidator<CreateTableDto>, CreateTableDtoValidator>();
            services.AddScoped<IValidator<Supplier>, SupplierValidator>();
            // User validators
            services.AddScoped<IValidator<AddUserCommand>, AddUserCommandValidator>();
            services.AddScoped<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();
            // Role validators
            services.AddScoped<IValidator<AddRoleCommand>, AddRoleCommandValidator>();
            // Setting validators
            services.AddScoped<IValidator<SaveSettingsCommand>, SaveSettingsCommandValidator>();
            // Supplier validators
            services.AddScoped<IValidator<SaveSupplierCommand>, SaveSupplierCommandValidator>();
            // CashSession validators
            services.AddScoped<IValidator<OpenSessionCommand>, OpenSessionCommandValidator>();
            services.AddScoped<IValidator<CloseSessionCommand>, CloseSessionCommandValidator>();
            // Purchase validators
            services.AddScoped<IValidator<SavePurchaseCommand>, SavePurchaseCommandValidator>();
            // Auth validators
            services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();

            // ── AutoMapper Configuration ──────────────────────────────────────────
            IMapper mapper = new AutoMapper.MapperConfiguration(
                mc => mc.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
                .CreateMapper();
            services.AddSingleton(mapper);
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IBIReportService, BIReportService>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ICashService, CashService>();
            services.AddScoped<ICategoryService>(provider => new CategoryService(provider.GetRequiredService<IMediator>()));
            services.AddScoped<ITableService>(provider => new TableService(provider.GetRequiredService<IMediator>()));
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPurchaseService>(provider => new PurchaseService(provider.GetRequiredService<IMediator>()));
            services.AddScoped<ISupplierService>(provider => new SupplierService(provider.GetRequiredService<IMediator>(), provider.GetRequiredService<IMapper>()));

            services.AddSingleton<ICartService, CartService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<IDialogService, Services.DialogService>();

            // ── ViewModels ────────────────────────────────────────────────────
            services.AddTransient<BillingViewModel>();
            services.AddTransient<SessionViewModel>();
            services.AddTransient<PurchaseEntryViewModel>();
            services.AddTransient<SupplierViewModel>();
            services.AddTransient<SupplierEntryViewModel>();
            services.AddTransient<PurchaseReportViewModel>();

            // ── Views & Windows ───────────────────────────────────────────────
            services.AddTransient<SessionView>();
            services.AddTransient<DashboardView>();
            services.AddTransient<ItemView>();
            services.AddTransient<CategoryView>();
            services.AddTransient<LedgerView>();
            services.AddTransient<JournalView>();
            services.AddTransient<SettingsView>();
            services.AddTransient<AuditView>();
            services.AddTransient<PurchaseEntryView>();
            services.AddTransient<BillingView>();
            services.AddTransient<SupplierView>();
            services.AddTransient<SalesReportView>();
            services.AddTransient<ItemReportView>();
            services.AddTransient<PurchaseReportView>();
            services.AddTransient<BIReportView>();
            services.AddTransient<TableView>();
            services.AddTransient<RolesView>();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OrderService).Assembly));

            // ── Windows & Views ──────────────────────────────────────────────
            // CLEANUP: Redundant individual view registrations were removed here as they are 
            // already registered in the "ViewModels & Views" section above.
            services.AddScoped<LoginWindow>();
            services.AddScoped<DashboardWindow>();
            services.AddTransient<AddItemWindow>();
            services.AddTransient<MainWindow>();
        }
    }
}
