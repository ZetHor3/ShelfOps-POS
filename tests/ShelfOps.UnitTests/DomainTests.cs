using ShelfOps.Domain.Models;
using ShelfOps.Domain.Enums;

namespace ShelfOps.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var product = new Product("ELEC-001", "Test Product", Guid.NewGuid(), 100m, 199.99m, "A test product", "123456789");

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("ELEC-001", product.Sku);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(100m, product.PurchasePrice);
        Assert.Equal(199.99m, product.SalePrice);
        Assert.Equal("A test product", product.Description);
        Assert.Equal("123456789", product.Barcode);
        Assert.True(product.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetSku_WithEmptyValue_Throws(string? sku)
    {
        var product = CreateValidProduct();
        Assert.Throws<ArgumentException>(() => product.SetSku(sku!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetName_WithEmptyValue_Throws(string? name)
    {
        var product = CreateValidProduct();
        Assert.Throws<ArgumentException>(() => product.SetName(name!));
    }

    [Fact]
    public void SetPurchasePrice_Negative_Throws()
    {
        var product = CreateValidProduct();
        Assert.Throws<ArgumentException>(() => product.SetPurchasePrice(-1));
    }

    [Fact]
    public void SetSalePrice_Zero_Throws()
    {
        var product = CreateValidProduct();
        Assert.Throws<ArgumentException>(() => product.SetSalePrice(0));
    }

    [Fact]
    public void SetSalePrice_Negative_Throws()
    {
        var product = CreateValidProduct();
        Assert.Throws<ArgumentException>(() => product.SetSalePrice(-10));
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var product = CreateValidProduct();
        product.Deactivate();
        Assert.False(product.IsActive);
    }

    [Fact]
    public void SetSku_TrimsValue()
    {
        var product = CreateValidProduct();
        product.SetSku("  NEW-SKU  ");
        Assert.Equal("NEW-SKU", product.Sku);
    }

    private static Product CreateValidProduct() =>
        new("SKU-001", "Default", Guid.NewGuid(), 50m, 99.99m);
}

public class SaleTests
{
    [Fact]
    public void Create_SetsDraftStatus()
    {
        var sale = new Sale(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.StartsWith("SALE-", sale.SaleNumber);
    }

    [Fact]
    public void AddItem_WhenDraft_AddsSuccessfully()
    {
        var sale = new Sale(Guid.NewGuid(), Guid.NewGuid());
        var item = new SaleItem(sale.Id, Guid.NewGuid(), "SKU", "Item", 2, 10m);

        sale.AddItem(item);

        Assert.Single(sale.Items);
    }

    [Fact]
    public void AddItem_WhenCompleted_Throws()
    {
        var sale = CreateCompletedSale();
        var item = new SaleItem(sale.Id, Guid.NewGuid(), "SKU", "Item", 1, 10m);

        Assert.Throws<InvalidOperationException>(() => sale.AddItem(item));
    }

    [Fact]
    public void Complete_FromDraft_SetsCompletedStatus()
    {
        var sale = new Sale(Guid.NewGuid(), Guid.NewGuid());
        sale.Complete(100m, 10m, 90m);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(100m, sale.Subtotal);
        Assert.Equal(10m, sale.DiscountTotal);
        Assert.Equal(90m, sale.GrandTotal);
        Assert.NotNull(sale.CompletedAtUtc);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_Throws()
    {
        var sale = CreateCompletedSale();
        Assert.Throws<InvalidOperationException>(() => sale.Complete(200m, 0, 200m));
    }

    [Fact]
    public void CalculateSubtotal_ReturnsSumOfLineTotals()
    {
        var sale = new Sale(Guid.NewGuid(), Guid.NewGuid());
        sale.AddItem(new SaleItem(sale.Id, Guid.NewGuid(), "SKU1", "Item1", 2, 10m));   // 20
        sale.AddItem(new SaleItem(sale.Id, Guid.NewGuid(), "SKU2", "Item2", 3, 15m));   // 45

        Assert.Equal(65m, sale.CalculateSubtotal());
    }

    [Fact]
    public void CalculateGrandTotal_SubtractsDiscount()
    {
        var sale = new Sale(Guid.NewGuid(), Guid.NewGuid());
        sale.Complete(100m, 15m, 0);

        Assert.Equal(85m, sale.CalculateGrandTotal());
    }

    private static Sale CreateCompletedSale()
    {
        var sale = new Sale(Guid.NewGuid(), Guid.NewGuid());
        sale.Complete(100m, 0, 100m);
        return sale;
    }
}

public class SaleItemTests
{
    [Fact]
    public void Create_CalculatesLineTotal()
    {
        var item = new SaleItem(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Name", 3, 10m);
        Assert.Equal(30m, item.LineTotal);
    }

    [Fact]
    public void Create_WithDiscount_SubtractsFromTotal()
    {
        var item = new SaleItem(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Name", 3, 10m, 5m);
        Assert.Equal(25m, item.LineTotal);
    }

    [Fact]
    public void Create_WithZeroQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SaleItem(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Name", 0, 10m));
    }

    [Fact]
    public void Create_WithNegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SaleItem(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Name", -1, 10m));
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SaleItem(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Name", 1, -5m));
    }

    [Fact]
    public void Create_WithDiscountExceedsTotal_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SaleItem(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Name", 1, 10m, 15m));
    }
}

public class StockMovementTests
{
    [Fact]
    public void Create_WithZeroDelta_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new StockMovement(Guid.NewGuid(), Guid.NewGuid(), MovementType.Receipt, 0,
                ReferenceType.Receipt, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithPositiveDelta_SetsProperties()
    {
        var movement = new StockMovement(Guid.NewGuid(), Guid.NewGuid(), MovementType.Receipt, 10,
            ReferenceType.Receipt, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(10, movement.QuantityDelta);
        Assert.Equal(MovementType.Receipt, movement.MovementType);
    }

    [Fact]
    public void Create_WithNegativeDelta_SetsProperties()
    {
        var movement = new StockMovement(Guid.NewGuid(), Guid.NewGuid(), MovementType.Sale, -5,
            ReferenceType.Sale, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(-5, movement.QuantityDelta);
        Assert.Equal(MovementType.Sale, movement.MovementType);
    }

    [Fact]
    public void Create_WithReason_SetsReason()
    {
        var movement = new StockMovement(Guid.NewGuid(), Guid.NewGuid(), MovementType.AdjustmentIncrease, 3,
            ReferenceType.Adjustment, Guid.NewGuid(), Guid.NewGuid(), "Inventory correction");

        Assert.Equal("Inventory correction", movement.Reason);
    }
}

public class StockBalanceTests
{
    [Fact]
    public void Create_WithNegativeInitial_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new StockBalance(Guid.NewGuid(), Guid.NewGuid(), -1));
    }

    [Fact]
    public void Create_WithZeroInitial_Succeeds()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), 0);
        Assert.Equal(0, balance.QuantityOnHand);
    }

    [Fact]
    public void Increase_AddsQuantity()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), 5);
        balance.Increase(10);
        Assert.Equal(15, balance.QuantityOnHand);
    }

    [Fact]
    public void Increase_WithNonPositive_Throws()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), 5);
        Assert.Throws<ArgumentException>(() => balance.Increase(0));
        Assert.Throws<ArgumentException>(() => balance.Increase(-1));
    }

    [Fact]
    public void Decrease_SubtractsQuantity()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), 10);
        balance.Decrease(4);
        Assert.Equal(6, balance.QuantityOnHand);
    }

    [Fact]
    public void Decrease_BelowZero_Throws()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), 3);
        Assert.Throws<InvalidOperationException>(() => balance.Decrease(5));
    }

    [Fact]
    public void Decrease_WithNonPositive_Throws()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), 5);
        Assert.Throws<ArgumentException>(() => balance.Decrease(0));
        Assert.Throws<ArgumentException>(() => balance.Decrease(-1));
    }
}

public class CustomerReturnTests
{
    [Fact]
    public void Create_SetsDraftStatus()
    {
        var ret = new CustomerReturn(Guid.NewGuid(), Guid.NewGuid(), "Defective");
        Assert.Equal(ReturnStatus.Draft, ret.Status);
        Assert.StartsWith("RET-", ret.ReturnNumber);
    }

    [Fact]
    public void Complete_FromDraft_Succeeds()
    {
        var ret = new CustomerReturn(Guid.NewGuid(), Guid.NewGuid(), "Defective");
        ret.Complete(50m);

        Assert.Equal(ReturnStatus.Completed, ret.Status);
        Assert.Equal(50m, ret.TotalAmount);
        Assert.NotNull(ret.CompletedAtUtc);
    }

    [Fact]
    public void Complete_WhenNotDraft_Throws()
    {
        var ret = new CustomerReturn(Guid.NewGuid(), Guid.NewGuid(), "Defective");
        ret.Complete(50m);
        Assert.Throws<InvalidOperationException>(() => ret.Complete(100m));
    }

    [Fact]
    public void AddItem_WhenDraft_Succeeds()
    {
        var ret = new CustomerReturn(Guid.NewGuid(), Guid.NewGuid(), "Defective");
        var item = new ReturnItem(ret.Id, Guid.NewGuid(), 1, 50m, RestockDisposition.Restock);
        ret.AddItem(item);
        Assert.Single(ret.Items);
    }

    [Fact]
    public void AddItem_WhenCompleted_Throws()
    {
        var ret = new CustomerReturn(Guid.NewGuid(), Guid.NewGuid(), "Defective");
        ret.Complete(50m);
        var item = new ReturnItem(ret.Id, Guid.NewGuid(), 1, 50m, RestockDisposition.Restock);
        Assert.Throws<InvalidOperationException>(() => ret.AddItem(item));
    }
}

public class ReturnItemTests
{
    [Fact]
    public void Create_CalculatesLineTotal()
    {
        var item = new ReturnItem(Guid.NewGuid(), Guid.NewGuid(), 3, 10m, RestockDisposition.Restock);
        Assert.Equal(30m, item.LineTotal);
    }

    [Fact]
    public void Create_WithZeroQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ReturnItem(Guid.NewGuid(), Guid.NewGuid(), 0, 10m, RestockDisposition.Restock));
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ReturnItem(Guid.NewGuid(), Guid.NewGuid(), 1, -5m, RestockDisposition.Restock));
    }
}

public class UserTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var user = new User("testuser", "Test User", "hash123", UserRole.Cashier);
        Assert.Equal("testuser", user.Username);
        Assert.Equal("Test User", user.DisplayName);
        Assert.Equal("hash123", user.PasswordHash);
        Assert.Equal(UserRole.Cashier, user.Role);
        Assert.True(user.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetUsername_WithEmptyValue_Throws(string? username)
    {
        var user = CreateValidUser();
        Assert.Throws<ArgumentException>(() => user.SetUsername(username!));
    }

    [Fact]
    public void SetUsername_NormalizesToLower()
    {
        var user = CreateValidUser();
        user.SetUsername("  AdminUser  ");
        Assert.Equal("adminuser", user.Username);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = CreateValidUser();
        user.Deactivate();
        Assert.False(user.IsActive);
    }

    [Fact]
    public void RecordLogin_SetsLastLogin()
    {
        var user = CreateValidUser();
        user.RecordLogin();
        Assert.NotNull(user.LastLoginAtUtc);
    }

    [Fact]
    public void SetRole_ChangesRole()
    {
        var user = CreateValidUser();
        user.SetRole(UserRole.Manager);
        Assert.Equal(UserRole.Manager, user.Role);
    }

    private static User CreateValidUser() => new("user", "User", "hash", UserRole.Cashier);
}

public class CategoryTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var category = new Category("Electronics", "Electronic items");
        Assert.Equal("Electronics", category.Name);
        Assert.Equal("Electronic items", category.Description);
        Assert.True(category.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetName_WithEmptyValue_Throws(string? name)
    {
        var category = new Category("Test", null);
        Assert.Throws<ArgumentException>(() => category.SetName(name!));
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var category = new Category("Test", null);
        category.Deactivate();
        Assert.False(category.IsActive);
    }
}

public class WarehouseTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var wh = new Warehouse("WH01", "Main Warehouse", "123 Street");
        Assert.Equal("WH01", wh.Code);
        Assert.Equal("Main Warehouse", wh.Name);
        Assert.Equal("123 Street", wh.Address);
        Assert.True(wh.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetCode_WithEmptyValue_Throws(string? code)
    {
        var wh = new Warehouse("WH01", "Main");
        Assert.Throws<ArgumentException>(() => wh.SetCode(code!));
    }

    [Fact]
    public void SetCode_Uppercases()
    {
        var wh = new Warehouse("wh01", "Main");
        Assert.Equal("WH01", wh.Code);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var wh = new Warehouse("WH01", "Main");
        wh.Deactivate();
        Assert.False(wh.IsActive);
    }
}

public class AuditLogTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var log = new AuditLog(Guid.NewGuid(), "LOGIN", "User", "user-123", "corr-1", "{}");

        Assert.NotEqual(Guid.Empty, log.Id);
        Assert.Equal("LOGIN", log.Action);
        Assert.Equal("User", log.EntityType);
        Assert.Equal("user-123", log.EntityId);
        Assert.Equal("corr-1", log.CorrelationId);
        Assert.Equal("{}", log.DetailsJson);
    }
}