using AutoMapper;
using FluentValidation;
using HotelPOS.Api;
using HotelPOS.Api.Configuration;
using HotelPOS.Api.Middleware;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Infrastructure;
using HotelPOS.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text;

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
builder.Services.AddScoped<IUnitOfMeasurementService>(provider => new UnitOfMeasurementService(provider.GetRequiredService<IMediator>()));
builder.Services.AddScoped<ITableService>(provider => new TableService(provider.GetRequiredService<IMediator>()));
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPurchaseService>(provider => new PurchaseService(provider.GetRequiredService<IMediator>()));
builder.Services.AddScoped<IEstimationService>(provider => new EstimationService(provider.GetRequiredService<IMediator>()));
builder.Services.AddScoped<IReservationService>(provider => new ReservationService(provider.GetRequiredService<IMediator>()));
builder.Services.AddScoped<ISupplierService>(provider => new SupplierService(provider.GetRequiredService<IMediator>(), provider.GetRequiredService<IMapper>()));
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<ITdsService, TdsService>();
builder.Services.AddScoped<IExpenseService>(provider => new ExpenseService(provider.GetRequiredService<IMediator>(), provider.GetRequiredService<IMapper>()));
builder.Services.AddScoped<IEmailService, HotelPOS.Infrastructure.Services.SmtpEmailService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

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
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
bool isCorsOriginAllowed(string? origin)
{
    if (string.IsNullOrWhiteSpace(origin)) return false;
    if (corsAllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;

    return builder.Environment.IsDevelopment()
        && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.Host is "localhost" or "127.0.0.1"
        && uri.Port is 4200 or 4201 or 4202 or 3000 or 5173 or 8080;
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.SetIsOriginAllowed(isCorsOriginAllowed)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});


// ── JWT Authentication ────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var jwtKey = jwtOptions.Key;
if (string.IsNullOrWhiteSpace(jwtKey))
    jwtKey = Environment.GetEnvironmentVariable("HOTELPOS_JWT_KEY");
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "JWT Key is not configured. Set Jwt:Key in appsettings or HOTELPOS_JWT_KEY environment variable.");

// ── PII column encryption key ─────────────────────────────────────────────
var piiKeyBase64 = builder.Configuration["Encryption:PiiKey"];
if (string.IsNullOrWhiteSpace(piiKeyBase64))
    piiKeyBase64 = Environment.GetEnvironmentVariable("HOTELPOS_PII_KEY");
if (string.IsNullOrWhiteSpace(piiKeyBase64))
    throw new InvalidOperationException(
        "PII encryption key is not configured. Set Encryption:PiiKey in appsettings or HOTELPOS_PII_KEY environment variable.");
try
{
    HotelPOS.Infrastructure.Persistence.PiiEncryptionKeyProvider.SetKey(Convert.FromBase64String(piiKeyBase64));
}
catch (FormatException ex)
{
    throw new InvalidOperationException("PII encryption key (Encryption:PiiKey / HOTELPOS_PII_KEY) must be valid Base64.", ex);
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Tokens are minted (AuthController) and read (ApiUserContext) using the raw JWT claim
        // names (JwtRegisteredClaimNames.Sub/UniqueName) directly. Without this, the default
        // inbound claim mapping silently renames "sub" to ClaimTypes.NameIdentifier before it
        // reaches HttpContext.User, so ApiUserContext.CurrentUserId's FindFirst("sub") lookup
        // never matches and always returns null - breaking every self-service check that
        // compares the caller's ID (self-delete guard, self-service password/2FA changes,
        // "own payslips" access) for every real login, not just tests.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ── Rate Limiting ─────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("AuthPolicy", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// ── OpenAPI ───────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowAngular");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();   // MUST come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can host this API in integration tests.</summary>
public partial class Program
{
    protected Program() { }
}
