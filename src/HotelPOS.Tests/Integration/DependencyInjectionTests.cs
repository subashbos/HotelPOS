using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Infrastructure;
using HotelPOS.Infrastructure.Persistence;
using HotelPOS.Services;
using HotelPOS.ViewModels;
using HotelPOS.Views;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using HotelPOS.Api.Controllers;
using HotelPOS.Api;
using MediatR;
using FluentValidation;
using AutoMapper;


namespace HotelPOS.Tests.Integration
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void Verify_Wpf_DependencyInjection_CanResolveAllServices()
        {
            var services = new ServiceCollection();

            // Setup fake configuration and DbContext
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=HotelPOS_DI_Test;Trusted_Connection=True;"
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddDbContext<HotelDbContext>(options =>
            {
                options.UseInMemoryDatabase("WpfDITestDb");
            });

            // Replicate registrations in App.xaml.cs
            services.AddLogging();
            services.AddSingleton<IUserContext, UserContext>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddInfrastructure();

            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IItemService, ItemService>();
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

            // ViewModels
            services.AddTransient<BillingViewModel>();
            services.AddTransient<SessionViewModel>();
            services.AddTransient<PurchaseEntryViewModel>();
            services.AddTransient<SupplierViewModel>();
            services.AddTransient<SupplierEntryViewModel>();
            services.AddTransient<PurchaseReportViewModel>();

            // MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OrderService).Assembly));

            // AutoMapper
            var mapperCfg = new AutoMapper.MapperConfiguration(
                mc => mc.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            services.AddSingleton<IMapper>(mapperCfg.CreateMapper());

            var provider = services.BuildServiceProvider();

            // Verify resolution of key services, viewmodels
            using (var scope = provider.CreateScope())
            {
                var errors = new List<string>();
                var typesToResolve = new[]
                {
                    typeof(IOrderService),
                    typeof(IItemService),
                    typeof(IAuthService),
                    typeof(IReportService),
                    typeof(IBIReportService),
                    typeof(ISettingService),
                    typeof(IUserService),
                    typeof(IAuditService),
                    typeof(ICashService),
                    typeof(ICategoryService),
                    typeof(ITableService),
                    typeof(IRoleService),
                    typeof(IPurchaseService),
                    typeof(ISupplierService),
                    typeof(BillingViewModel),
                    typeof(SessionViewModel),
                    typeof(PurchaseEntryViewModel),
                    typeof(SupplierViewModel),
                    typeof(SupplierEntryViewModel),
                    typeof(PurchaseReportViewModel)
                };

                foreach (var type in typesToResolve)
                {
                    try
                    {
                        scope.ServiceProvider.GetRequiredService(type);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to resolve {type.Name}: {ex.Message}");
                    }
                }

                Assert.Empty(errors);
            }
        }

        [Fact]
        public void Verify_Api_DependencyInjection_CanResolveAllServices()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=HotelPOS_DI_Test;Trusted_Connection=True;",
                    ["Jwt:Key"] = "HotelPOS_TestJwtKey_Minimum32Characters!",
                    ["Jwt:Issuer"] = "HotelPOS",
                    ["Jwt:Audience"] = "HotelPOSClient"
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddDbContext<HotelDbContext>(options =>
            {
                options.UseInMemoryDatabase("ApiDITestDb");
            });

            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddScoped<IUserContext, ApiUserContext>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddInfrastructure();

            services.AddScoped<HotelPOS.Application.Interfaces.IAuthService, HotelPOS.Application.UseCases.AuthService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IBIReportService, BIReportService>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICashService, CashService>();
            services.AddScoped<ICategoryService>(provider => new CategoryService(provider.GetRequiredService<IMediator>()));
            services.AddScoped<ITableService>(provider => new TableService(provider.GetRequiredService<IMediator>()));
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPurchaseService>(provider => new PurchaseService(provider.GetRequiredService<IMediator>()));
            services.AddScoped<ISupplierService>(provider => new SupplierService(provider.GetRequiredService<IMediator>(), provider.GetRequiredService<IMapper>()));

            // AutoMapper
            var mapperCfg = new AutoMapper.MapperConfiguration(
                mc =>
                {
                    mc.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile());
                    mc.CreateMap<HotelPOS.Api.Controllers.CreateItemRequest, HotelPOS.Application.UseCases.Items.Commands.CreateItemCommand>();
                    mc.CreateMap<HotelPOS.Api.Controllers.CreateOrderRequest, HotelPOS.Application.UseCases.Orders.Commands.CreateOrderCommand>();
                },
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            AutoMapper.IMapper mapper = mapperCfg.CreateMapper();
            services.AddSingleton(mapper);

            // Controllers
            services.AddTransient<AuthController>();
            services.AddTransient<ItemsController>();
            services.AddTransient<OrdersController>();

            // MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(ItemService).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(HotelPOS.Application.Common.Behaviors.ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(HotelPOS.Application.UseCases.Items.Commands.CreateItemCommandValidator).Assembly);

            var provider = services.BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var errors = new List<string>();
                var typesToResolve = new[]
                {
                    typeof(AuthController),
                    typeof(ItemsController),
                    typeof(OrdersController)
                };

                foreach (var type in typesToResolve)
                {
                    try
                    {
                        scope.ServiceProvider.GetRequiredService(type);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to resolve API Controller {type.Name}: {ex.Message}");
                    }
                }

                Assert.Empty(errors);
            }
        }
    }
}
