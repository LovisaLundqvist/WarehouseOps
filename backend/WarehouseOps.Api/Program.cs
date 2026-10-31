using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WarehouseOps.Api.Middleware;
using WarehouseOps.Api.Services;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Infrastructure;
using WarehouseOps.Infrastructure.Repositories;
using WarehouseOps.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "WarehouseOpsFrontend";

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste the JWT token here. Swagger will send it as a Bearer token.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document =>
    {
        return new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        };
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Five login attempts per minute and client IP. No waiting queue is permitted.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, _) =>
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.Headers["Retry-After"] = "60";
        await response.WriteAsJsonAsync(new
        {
            message = "Too many login attempts. Please try again later."
        });
    };
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];

if (string.IsNullOrWhiteSpace(jwtSecretKey) || jwtSecretKey.Length < 32)
{
    throw new InvalidOperationException("JWT secret key is missing or too short.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WarehouseOps.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WarehouseOps.Client";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITransactionExecutor, EfTransactionExecutor>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();

// Business rules stay in the inner services. Decorators handle transactions.
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductService>(provider =>
    new TransactionalProductService(
        provider.GetRequiredService<ProductService>(),
        provider.GetRequiredService<ITransactionExecutor>()));
builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<IInventoryService>(provider =>
    new TransactionalInventoryService(
        provider.GetRequiredService<InventoryService>(),
        provider.GetRequiredService<ITransactionExecutor>()));
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ICustomerService>(provider =>
    new TransactionalCustomerService(
        provider.GetRequiredService<CustomerService>(),
        provider.GetRequiredService<ITransactionExecutor>()));
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderService>(provider =>
    new TransactionalOrderService(
        provider.GetRequiredService<OrderService>(),
        provider.GetRequiredService<ITransactionExecutor>()));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<ShipmentService>();
builder.Services.AddScoped<IShipmentService>(provider =>
    new TransactionalShipmentService(
        provider.GetRequiredService<ShipmentService>(),
        provider.GetRequiredService<ITransactionExecutor>()));
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();

builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<IIncidentService>(provider =>
    new TransactionalIncidentService(
        provider.GetRequiredService<IncidentService>(),
        provider.GetRequiredService<ITransactionExecutor>()));
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    await ApplyDatabaseMigrationsAsync(app);
}

if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
{
    await SeedDemoDataAsync(app);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(frontendCorsPolicy);

// Endpoint-specific rate limiting must run after routing.
app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.MigrateAsync();

            logger.LogInformation("Database migrations were applied successfully.");

            return;
        }
        catch (Exception exception) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                exception,
                "Database migration attempt {Attempt} of {MaxAttempts} failed. Retrying in 5 seconds.",
                attempt,
                maxAttempts);

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    await dbContext.Database.MigrateAsync();
}

static async Task SeedDemoDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await DatabaseSeeder.SeedDemoDataAsync(dbContext);

    logger.LogInformation("Demo seed data was checked successfully.");
}

// Expose the web entry point to WebApplicationFactory integration tests.
public partial class Program { }
