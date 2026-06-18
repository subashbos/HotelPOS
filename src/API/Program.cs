using HotelPOS.Api;
using HotelPOS.Api.Middleware;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Infrastructure;
using HotelPOS.Infrastructure.Persistence;
using MediatR;
using AutoMapper;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers with automatic model validation responses ─────────────────
builder.Services.AddControllers();

// ── Database Configuration ────────────────────────────────────────────────
builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Dependency Injection ──────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, ApiUserContext>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddInfrastructure();

builder.Services.AddScoped<HotelPOS.Application.Interfaces.IAuthService, HotelPOS.Application.UseCases.AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IBIReportService, BIReportService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICashService, CashService>();
builder.Services.AddScoped<ICategoryService>(provider => new CategoryService(provider.GetRequiredService<IMediator>()));
builder.Services.AddScoped<ITableService>(provider => new TableService(provider.GetRequiredService<IMediator>()));
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPurchaseService>(provider => new PurchaseService(provider.GetRequiredService<IMediator>()));
builder.Services.AddScoped<ISupplierService>(provider => new SupplierService(provider.GetRequiredService<IMediator>(), provider.GetRequiredService<IMapper>()));

// ── MediatR Configuration ─────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(HotelPOS.Application.UseCases.ItemService).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(HotelPOS.Application.Common.Behaviors.ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(HotelPOS.Application.UseCases.Items.Commands.CreateItemCommandValidator).Assembly);

// ── AutoMapper Configuration ──────────────────────────────────────────
var mapperCfg = new AutoMapper.MapperConfiguration(
    mc =>
    {
        mc.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile());
        mc.CreateMap<HotelPOS.Api.Controllers.CreateItemRequest, HotelPOS.Application.UseCases.Items.Commands.CreateItemCommand>();
        mc.CreateMap<HotelPOS.Api.Controllers.CreateOrderRequest, HotelPOS.Application.UseCases.Orders.Commands.CreateOrderCommand>();
    },
    Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
AutoMapper.IMapper mapper = mapperCfg.CreateMapper();
builder.Services.AddSingleton(mapper);

// ── CORS Configuration ────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── JWT Authentication ────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("HOTELPOS_JWT_KEY");
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "JWT Key is not configured. Set Jwt:Key in appsettings or HOTELPOS_JWT_KEY environment variable.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HotelPOS";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HotelPOSClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ── OpenAPI ───────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowAngular");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();   // MUST come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
