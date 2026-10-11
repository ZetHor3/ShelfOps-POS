using Moq;
using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;
using ShelfOps.Application.Services;
using ShelfOps.Domain.Enums;
using ShelfOps.Domain.Models;

namespace ShelfOps.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        Func<string, string> hash = p => $"HASHED_{p}";
        Func<string, string, bool> verify = (p, h) => h == $"HASHED_{p}";
        Func<User, (string, DateTime)> genToken = u => ("token123", DateTime.UtcNow.AddHours(1));

        _service = new AuthService(_userRepo.Object, _uow.Object, hash, verify, genToken);
    }

    [Fact]
    public async Task Login_WithEmptyUsername_ReturnsFailure()
    {
        var result = await _service.LoginAsync(new LoginRequest("", "pass"));
        Assert.True(result.IsFailure);
        Assert.Equal("Username and password are required.", result.Error);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ReturnsFailure()
    {
        var result = await _service.LoginAsync(new LoginRequest("user", ""));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsFailure()
    {
        _userRepo.Setup(r => r.GetByUsernameAsync("nobody", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.LoginAsync(new LoginRequest("nobody", "pass"));
        Assert.True(result.IsFailure);
        Assert.Equal("Invalid credentials.", result.Error);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsFailure()
    {
        var user = CreateUser("inactive");
        user.Deactivate();
        _userRepo.Setup(r => r.GetByUsernameAsync("inactive", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest("inactive", "pass"));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsFailure()
    {
        var user = CreateUser("testuser", "HASHED_correctpass");
        _userRepo.Setup(r => r.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest("testuser", "wrongpass"));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        var user = CreateUser("testuser", "HASHED_correctpass");
        _userRepo.Setup(r => r.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest("testuser", "correctpass"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("token123", result.Value.Token);
        Assert.Equal("testuser", result.Value.Username);
        Assert.Equal("Test User", result.Value.DisplayName);
        Assert.Equal("Cashier", result.Value.Role);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static User CreateUser(string username, string hash = "hash") =>
        new(username, "Test User", hash, UserRole.Cashier);
}

public class SaleServiceTests
{
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockBalanceRepository> _stockRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly SaleService _service;

    public SaleServiceTests()
    {
        // Every CompleteSale code path calls NextSaleNumberAsync; return a dummy
        // number so SetSaleNumber doesn't throw on null.
        _saleRepo.Setup(r => r.NextSaleNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SALE-20261011-99999");

        _service = new SaleService(
            _saleRepo.Object, _productRepo.Object, _stockRepo.Object,
            _movementRepo.Object, _auditRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task CompleteSale_WithEmptyItems_ReturnsFailure()
    {
        var request = new CompleteSaleRequest(Guid.NewGuid(), Guid.NewGuid(), new List<AddSaleItemRequest>());

        var result = await _service.CompleteSaleAsync(request, Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Sale must have at least one item.", result.Error);
    }

    [Fact]
    public async Task CompleteSale_WithIdempotencyKey_ReturnsExisting()
    {
        var saleId = Guid.NewGuid();
        var existing = new Sale(Guid.NewGuid(), Guid.NewGuid());
        _saleRepo.Setup(r => r.GetByIdempotencyKeyAsync("dup-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CompleteSaleRequest(saleId, Guid.NewGuid(),
            new List<AddSaleItemRequest> { new(Guid.NewGuid(), 1) }, IdempotencyKey: "dup-key");

        var result = await _service.CompleteSaleAsync(request, Guid.NewGuid(), default);

        Assert.True(result.IsSuccess);
        _saleRepo.Verify(r => r.GetByIdempotencyKeyAsync("dup-key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteSale_WithUnknownProduct_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _uow.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((fn, _) => fn());

        var request = new CompleteSaleRequest(Guid.NewGuid(), Guid.NewGuid(),
            new List<AddSaleItemRequest> { new(productId, 1) });

        var result = await _service.CompleteSaleAsync(request, Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task CompleteSale_WithInactiveProduct_ReturnsFailure()
    {
        var product = new Product("SKU", "Inactive", Guid.NewGuid(), 10m, 20m);
        product.Deactivate();
        var productId = product.Id;
        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _uow.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((fn, _) => fn());

        var request = new CompleteSaleRequest(Guid.NewGuid(), Guid.NewGuid(),
            new List<AddSaleItemRequest> { new(productId, 1) });

        var result = await _service.CompleteSaleAsync(request, Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Contains("not active", result.Error);
    }

    [Fact]
    public async Task CompleteSale_WithInsufficientStock_ReturnsFailure()
    {
        var product = new Product("SKU", "Active", Guid.NewGuid(), 10m, 20m);
        var productId = product.Id;
        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _stockRepo.Setup(r => r.GetAsync(It.IsAny<Guid>(), productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockBalance?)null);
        _uow.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((fn, _) => fn());

        var request = new CompleteSaleRequest(Guid.NewGuid(), Guid.NewGuid(),
            new List<AddSaleItemRequest> { new(productId, 1) });

        var result = await _service.CompleteSaleAsync(request, Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Contains("Insufficient stock", result.Error);
    }
}

public class ReturnServiceTests
{
    private readonly Mock<IReturnRepository> _returnRepo = new();
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockBalanceRepository> _stockRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ReturnService _service;

    public ReturnServiceTests()
    {
        _service = new ReturnService(
            _returnRepo.Object, _saleRepo.Object, _productRepo.Object,
            _stockRepo.Object, _movementRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task CreateReturn_WithUnknownSale_ReturnsFailure()
    {
        _saleRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale?)null);

        var result = await _service.CreateReturnAsync(
            new CreateReturnRequest(Guid.NewGuid(), "Defective",
                new List<ReturnItemRequest> { new(Guid.NewGuid(), 1, "Restock") }), Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Sale not found.", result.Error);
    }

    [Fact]
    public async Task CreateReturn_WithDraftSale_ReturnsFailure()
    {
        var sale = new Sale(Guid.NewGuid(), Guid.NewGuid());
        _saleRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sale);

        var result = await _service.CreateReturnAsync(
            new CreateReturnRequest(Guid.NewGuid(), "Defective",
                new List<ReturnItemRequest> { new(Guid.NewGuid(), 1, "Restock") }), Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Can only return completed sales.", result.Error);
    }

    [Fact]
    public async Task CreateReturn_WithUnknownSaleItem_ReturnsFailure()
    {
        var sale = new Sale(Guid.NewGuid(), Guid.NewGuid());
        sale.Complete(100m, 0, 100m);
        _saleRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sale);

        var result = await _service.CreateReturnAsync(
            new CreateReturnRequest(Guid.NewGuid(), "Defective",
                new List<ReturnItemRequest> { new(Guid.NewGuid(), 1, "Restock") }), Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error);
    }
}

public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly UserManagementService _service;

    public UserManagementServiceTests()
    {
        Func<string, string> hash = p => $"HASHED_{p}";
        _service = new UserManagementService(_userRepo.Object, hash, _uow.Object);
    }

    [Fact]
    public async Task CreateUser_WithEmptyUsername_ReturnsFailure()
    {
        var result = await _service.CreateUserAsync(
            new CreateUserRequest("", "Test", "pass", "Cashier"), default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateUser_WithEmptyPassword_ReturnsFailure()
    {
        var result = await _service.CreateUserAsync(
            new CreateUserRequest("test", "Test", "", "Cashier"), default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ReturnsFailure()
    {
        _userRepo.Setup(r => r.IsUsernameUniqueAsync("existing", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.CreateUserAsync(
            new CreateUserRequest("existing", "Test", "pass123", "Manager"), default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsSuccess()
    {
        _userRepo.Setup(r => r.IsUsernameUniqueAsync("newuser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateUserAsync(
            new CreateUserRequest("newuser", "New User", "pass123", "Cashier"), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("newuser", result.Value.Username);
        Assert.Equal("New User", result.Value.DisplayName);
        _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUser_WithUnknownUser_ReturnsFailure()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.DeactivateUserAsync(Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("User not found.", result.Error);
    }

    [Fact]
    public async Task DeactivateUser_WithValidUser_Succeeds()
    {
        var user = new User("test", "Test", "hash", UserRole.Cashier);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.DeactivateUserAsync(user.Id, default);

        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeUserRole_WithInvalidRole_ReturnsFailure()
    {
        var result = await _service.ChangeUserRoleAsync(Guid.NewGuid(), "InvalidRole", default);

        Assert.True(result.IsFailure);
    }
}