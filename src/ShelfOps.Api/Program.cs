using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ShelfOps.Application.Interfaces;
using ShelfOps.Application.Services;
using ShelfOps.Infrastructure.Data;
using ShelfOps.Infrastructure.Data.Repositories;
using ShelfOps.Infrastructure.Data.Seed;
using ShelfOps.Infrastructure.Security;
using ShelfOps.Infrastructure.Services;
using ShelfOps.Api.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ───
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Database ───
builder.Services.AddDbContext<ShelfOpsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Repositories ───
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IStockBalanceRepository, StockBalanceRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IReturnRepository, ReturnRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ─── Services ───
// Auth uses Func delegates for cross-layer concern injection
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>(sp =>
{
    var userRepo = sp.GetRequiredService<IUserRepository>();
    var uow = sp.GetRequiredService<IUnitOfWork>();
    var hasher = sp.GetRequiredService<IPasswordHasher>();
    var jwt = sp.GetRequiredService<IJwtTokenService>();
    return new AuthService(
        userRepo, uow,
        hasher.Hash,
        hasher.Verify,
        jwt.GenerateToken);
});
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>(sp =>
{
    var userRepo = sp.GetRequiredService<IUserRepository>();
    var uow = sp.GetRequiredService<IUnitOfWork>();
    var hasher = sp.GetRequiredService<IPasswordHasher>();
    return new UserManagementService(userRepo,
        password => hasher.Hash(password), uow);
});
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ─── JWT Authentication ───
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ─── Controllers + FluentValidation ───
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(ShelfOps.Application.DTOs.LoginRequest).Assembly);

// ─── Swagger ───
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── CORS ───
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDesktop", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ─── Middleware ───
app.UseExceptionHandling();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowDesktop");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── Seed Data ───
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShelfOpsDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(db, hasher);
}

app.Run();

public partial class Program { }