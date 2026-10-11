using ShelfOps.Domain.ValueObjects;
using ShelfOps.Application.Common;

namespace ShelfOps.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Zero_ReturnsZero()
    {
        Assert.Equal(0m, Money.Zero.Amount);
    }

    [Fact]
    public void Add_ReturnsSum()
    {
        var a = new Money(100m);
        var b = new Money(50m);
        var result = a.Add(b);
        Assert.Equal(150m, result.Amount);
    }

    [Fact]
    public void Subtract_ReturnsDifference()
    {
        var a = new Money(100m);
        var b = new Money(30m);
        var result = a.Subtract(b);
        Assert.Equal(70m, result.Amount);
    }

    [Fact]
    public void Multiply_ReturnsProduct()
    {
        var m = new Money(25m);
        var result = m.Multiply(3m);
        Assert.Equal(75m, result.Amount);
    }
}

public class QuantityTests
{
    [Fact]
    public void Zero_ReturnsZero()
    {
        Assert.Equal(0, Quantity.Zero.Value);
    }

    [Fact]
    public void IsPositive_PositiveValue_ReturnsTrue()
    {
        var q = new Quantity(5);
        Assert.True(q.IsPositive);
        Assert.False(q.IsNegative);
        Assert.False(q.IsZero);
    }

    [Fact]
    public void IsNegative_NegativeValue_ReturnsTrue()
    {
        var q = new Quantity(-3);
        Assert.True(q.IsNegative);
        Assert.False(q.IsPositive);
        Assert.False(q.IsZero);
    }

    [Fact]
    public void IsZero_Zero_ReturnsTrue()
    {
        var q = Quantity.Zero;
        Assert.True(q.IsZero);
    }

    [Fact]
    public void Add_ReturnsSum()
    {
        var result = new Quantity(10).Add(new Quantity(5));
        Assert.Equal(15, result.Value);
    }

    [Fact]
    public void Subtract_ReturnsDifference()
    {
        var result = new Quantity(10).Subtract(new Quantity(3));
        Assert.Equal(7, result.Value);
    }
}

public class PagedResultTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var items = new List<int> { 1, 2, 3 };
        var result = new PagedResult<int>(items, 50, 2, 10);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        var result = new PagedResult<int>(new List<int>(), 25, 1, 10);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ExactDivision()
    {
        var result = new PagedResult<int>(new List<int>(), 20, 1, 10);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public void HasPreviousPage_FirstPage_ReturnsFalse()
    {
        var result = new PagedResult<int>(new List<int>(), 50, 1, 10);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_SecondPage_ReturnsTrue()
    {
        var result = new PagedResult<int>(new List<int>(), 50, 2, 10);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_LastPage_ReturnsFalse()
    {
        var result = new PagedResult<int>(new List<int>(), 20, 2, 10);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void HasNextPage_NotLastPage_ReturnsTrue()
    {
        var result = new PagedResult<int>(new List<int>(), 25, 2, 10);
        Assert.True(result.HasNextPage);
    }
}

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessResult()
    {
        var result = Result<string>.Success("hello");
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("hello", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CreatesFailureResult()
    {
        var result = Result<string>.Failure("Something went wrong");
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Equal("Something went wrong", result.Error);
    }

    [Fact]
    public void Void_Success_CreatesSuccess()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Void_Failure_CreatesFailure()
    {
        var result = Result.Failure("Error");
        Assert.True(result.IsFailure);
        Assert.Equal("Error", result.Error);
    }
}