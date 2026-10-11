namespace ShelfOps.Application.DTOs
{
    public record CreateProductRequest(
        string Sku,
        string Name,
        Guid CategoryId,
        decimal PurchasePrice,
        decimal SalePrice,
        string? Description = null,
        string? Barcode = null);

    public record UpdateProductRequest(
        string Name,
        Guid CategoryId,
        decimal PurchasePrice,
        decimal SalePrice,
        string? Description = null,
        string? Barcode = null);

    public record ProductResponse(
        Guid Id,
        string Sku,
        string Name,
        Guid CategoryId,
        string? CategoryName,
        string? Description,
        string? Barcode,
        decimal PurchasePrice,
        decimal SalePrice,
        bool IsActive,
        int StockTotal,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    public record ProductStockResponse(
        Guid ProductId,
        Guid WarehouseId,
        string? WarehouseName,
        int QuantityOnHand);
}