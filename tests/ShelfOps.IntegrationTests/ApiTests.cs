using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ShelfOps.Application.DTOs;
using ShelfOps.Infrastructure.Data;
using ShelfOps.Infrastructure.Data.Seed;
using ShelfOps.Infrastructure.Security;

namespace ShelfOps.IntegrationTests;

public class ShelfOpsWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Development");
        builder.ConfigureServices(services =>
        {
            // Remove all EF Core registrations (Npgsql) so only InMemory is registered
            var efDescriptors = services
                .Where(d => d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore",
                    StringComparison.Ordinal) == true)
                .ToList();
            foreach (var descriptor in efDescriptors)
                services.Remove(descriptor);

            // Add InMemory database for testing
            services.AddDbContext<ShelfOpsDbContext>(options =>
                options.UseInMemoryDatabase("ShelfOpsTestDb")
                       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }
}

public class IntegrationTestsBase : IClassFixture<ShelfOpsWebFactory>
{
    protected readonly HttpClient Client;
    protected readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IntegrationTestsBase(ShelfOpsWebFactory factory)
    {
        Client = factory.CreateClient();
    }

    protected async Task<string> GetAdminTokenAsync()
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/login",
            new LoginRequest("admin", "Admin@123"));
        response.EnsureSuccessStatusCode();
        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return loginResult!.Token;
    }

    protected async Task<string> GetCashierTokenAsync()
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/login",
            new LoginRequest("cashier", "Cashier@123"));
        response.EnsureSuccessStatusCode();
        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return loginResult!.Token;
    }

    protected void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Remove("Authorization");
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }
}

public class AuthEndpointTests : IntegrationTestsBase
{
    public AuthEndpointTests(ShelfOpsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/login",
            new LoginRequest("admin", "Admin@123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("admin", result.Username);
        Assert.Equal("Administrator", result.Role);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/login",
            new LoginRequest("admin", "wrongpassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/login",
            new LoginRequest("nobody", "password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserInfo()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Authorization");
        var response = await Client.GetAsync("api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class ProductsEndpointTests : IntegrationTestsBase
{
    public ProductsEndpointTests(ShelfOpsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProducts_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("api/v1/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithAuth_ReturnsPagedResults()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("api/v1/products?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<ProductResponse>>(JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task GetProducts_SearchBySku_ReturnsFilteredResults()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("api/v1/products?search=SKU-001&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<ProductResponse>>(JsonOptions);
        Assert.NotNull(result);
        Assert.All(result.Items, p => Assert.Contains("SKU-001", p.Sku, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetProductById_WithValidId_ReturnsProduct()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        // Get first product to find its ID
        var listResponse = await Client.GetAsync("api/v1/products?page=1&pageSize=1");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<ProductResponse>>(JsonOptions);
        var firstId = list!.Items[0].Id;

        var response = await Client.GetAsync($"api/v1/products/{firstId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);
        Assert.Equal(firstId, product.Id);
    }

    [Fact]
    public async Task GetProductById_WithInvalidId_ReturnsNotFound()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync($"api/v1/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class CategoriesEndpointTests : IntegrationTestsBase
{
    public CategoriesEndpointTests(ShelfOpsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task GetCategories_ReturnsList()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("api/v1/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(JsonOptions);
        Assert.NotNull(categories);
        Assert.NotEmpty(categories);
    }
}

public class WarehousesEndpointTests : IntegrationTestsBase
{
    public WarehousesEndpointTests(ShelfOpsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task GetWarehouses_ReturnsList()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("api/v1/warehouses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var warehouses = await response.Content.ReadFromJsonAsync<List<WarehouseResponse>>(JsonOptions);
        Assert.NotNull(warehouses);
        Assert.NotEmpty(warehouses);
    }
}

public class SalesEndpointTests : IntegrationTestsBase
{
    public SalesEndpointTests(ShelfOpsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task GetSales_ReturnsPagedResults()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("api/v1/sales?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<SaleResponse>>(JsonOptions);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CompleteSale_WithValidData_ReturnsSuccess()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        // Find a product that has stock in some warehouse
        var prodResponse = await Client.GetAsync("api/v1/products?page=1&pageSize=50");
        var prodData = await prodResponse.Content.ReadFromJsonAsync<PagedResultDto<ProductResponse>>(JsonOptions);

        Guid? productWithStock = null;
        Guid? warehouseForStock = null;
        int quantityBefore = 0;

        foreach (var p in prodData!.Items)
        {
            var stockResponse = await Client.GetAsync($"api/v1/products/{p.Id}/stock");
            if (!stockResponse.IsSuccessStatusCode) continue;
            var stock = await stockResponse.Content.ReadFromJsonAsync<List<ProductStockResponse>>(JsonOptions);
            if (stock != null && stock.Any(s => s.QuantityOnHand > 0))
            {
                productWithStock = p.Id;
                var withStock = stock.First(s => s.QuantityOnHand > 0);
                warehouseForStock = withStock.WarehouseId;
                quantityBefore = withStock.QuantityOnHand;
                break;
            }
        }

        Assert.NotNull(productWithStock);

        var request = new CompleteSaleRequestDto(
            Guid.NewGuid(), warehouseForStock!.Value,
            new List<AddSaleItemRequestDto> { new(productWithStock!.Value, 1, 0) });

        var response = await Client.PostAsJsonAsync("api/v1/sales/complete", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>(JsonOptions);
        Assert.NotNull(sale);
        Assert.Equal("Completed", sale.Status);
        Assert.True(sale.GrandTotal > 0, "GrandTotal should be > 0");

        // Regression guard: the sale transaction must actually persist.
        // History: CommitAsync used to run without SaveChanges, so the sale
        // reported Completed while stock_balances never changed.
        var stockAfterResponse = await Client.GetAsync($"api/v1/products/{productWithStock.Value}/stock");
        var stockAfter = await stockAfterResponse.Content.ReadFromJsonAsync<List<ProductStockResponse>>(JsonOptions);
        var afterForWarehouse = stockAfter!.First(s => s.WarehouseId == warehouseForStock!.Value);
        Assert.Equal(quantityBefore - 1, afterForWarehouse.QuantityOnHand);
    }

    [Fact]
    public async Task CompleteSale_WithInsufficientStock_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var warehouseResponse = await Client.GetAsync("api/v1/warehouses");
        var warehouses = await warehouseResponse.Content.ReadFromJsonAsync<List<WarehouseResponse>>(JsonOptions);

        var request = new CompleteSaleRequestDto(
            Guid.NewGuid(), warehouses![0].Id,
            new List<AddSaleItemRequestDto> { new(Guid.NewGuid(), 999, 0) });

        var response = await Client.PostAsJsonAsync("api/v1/sales/complete", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class UsersEndpointTests : IntegrationTestsBase
{
    public UsersEndpointTests(ShelfOpsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateUser_ByAdmin_Succeeds()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var request = new CreateUserRequest("newtest", "New Test", "Pass123!", "Cashier");

        var response = await Client.PostAsJsonAsync("api/v1/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal("newtest", user.Username);
    }
}

public class ReportsEndpointTests : IntegrationTestsBase
{
    public ReportsEndpointTests(ShelfOpsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task GetDashboard_ReturnsDashboardData()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("api/v1/reports/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponseDto>(JsonOptions);
        Assert.NotNull(dashboard);
    }
}

// ─── DTOs matching the API response shapes ───

public record PagedResultDto<T>(List<T> Items, int TotalCount, int Page, int PageSize);
public record LoginResponse(string Token, string Username, string DisplayName, string Role, Guid UserId, DateTime ExpiresAt);
public record ProductResponse(Guid Id, string Sku, string Name, Guid CategoryId, string? CategoryName, string? Description, string? Barcode, decimal PurchasePrice, decimal SalePrice, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public record CategoryResponse(Guid Id, string Name, string? Description, bool IsActive);
public record WarehouseResponse(Guid Id, string Code, string Name, string? Address);
public record SaleResponse(Guid Id, string SaleNumber, Guid WarehouseId, string? WarehouseName, Guid CashierUserId, string? CashierName, string Status, decimal Subtotal, decimal DiscountTotal, decimal GrandTotal, DateTime CreatedAtUtc, DateTime? CompletedAtUtc, List<SaleItemResponseDto> Items);
public record SaleItemResponseDto(Guid Id, Guid ProductId, string ProductSku, string ProductName, int Quantity, decimal UnitPrice, decimal DiscountAmount, decimal LineTotal);
public record CompleteSaleRequestDto(Guid SaleId, Guid WarehouseId, List<AddSaleItemRequestDto> Items, string? IdempotencyKey = null);
public record AddSaleItemRequestDto(Guid ProductId, int Quantity, decimal DiscountAmount = 0);
public record UserResponse(Guid Id, string Username, string DisplayName, string Role, bool IsActive, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc);
public record DashboardResponseDto(decimal TodayRevenue, decimal PeriodRevenue, int SalesCount, decimal AverageReceipt, int ActiveProducts, decimal StockValue, int LowStockCount, List<decimal> RevenueDynamics, List<SaleResponse> RecentSales);